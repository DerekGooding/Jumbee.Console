namespace Jumbee.Console;

using ConsoleGUI;
using ConsoleGUI.Api;
using ConsoleGUI.Input;
using ConsoleGUI.Space;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;   

/// <summary>
/// Manages the overall UI and provides a paint event for controls to subscribe to.
/// </summary>
public static class UI
{
    #region Methods
    /// <summary>
    /// Initializes the console and starts the UI.
    /// </summary>
    public static Task Start(ILayout layout, int width = 110, int height = 25, int paintInterval = 100, bool isAnsiTerminal = true, IConsole? console = null, IInputSource? input = null)
    {
        if (isRunning) return runCompletion.Task;
        ProcessMetrics.Start();
        // Drives the renderer: ANSI escape sequences when true, IConsole.Write (16-colour System.Console) when false.
        ConsoleManager.AnsiEnabled = isAnsiTerminal;
        if (console != null)
        {
            ConsoleManager.Console = console;
        }
        else if (!isAnsiTerminal)
        {
            // Legacy terminal: 16-colour System.Console output. Input stays keyboard-only (ConsoleInputSource
            // below) since VT mouse/paste/focus aren't available.
            ConsoleManager.Console = new SimplifiedConsole();
        }
        inputSource = input ?? new ConsoleInputSource();
        ConsoleManager.Setup();
        ConsoleManager.Resize(new Size(width, height));
        ConsoleManager.Content = layout.CControl;
        UI.layout = layout;
        foreach(var c in layout.Controls.Select(lc => lc.FocusableControl))
        {
            if (!controls.Contains(c))
            {
                controls.Add(c);
            }               
        }
        interval = paintInterval;
        cts = new CancellationTokenSource();
        cancellationToken = cts.Token;
        runCompletion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        isRunning = true;
        // Start the single UI thread (reads the dispatcher queue, then renders each frame).
        dispatcher.Start(OnFrame, interval);
        // Start the input thread which blocks on the console and marshals each key onto the UI thread.
        inputThread = new Thread(InputLoop) { IsBackground = true, Name = "Jumbee.Console.Input" };
        inputThread.Start();
        return runCompletion.Task;
    }

    /// <summary>
    /// Reads console keys on a dedicated thread and posts their dispatch onto the UI thread so input is
    /// handled on the same thread as rendering.
    /// </summary>
    private static void InputLoop()
    {
        while (isRunning && !cancellationToken.IsCancellationRequested)
        {
            if (inputSource.TryRead(out var evt))
            {
                var e = evt;
                dispatcher.Post(() => OnInput(e));
            }
            else
            {
                Thread.Sleep(interval / 4);
            }
        }
    }

    /// <summary>Dispatches a terminal input event on the UI thread, routing by kind.</summary>
    private static void OnInput(TerminalInputEvent? evt)
    {
        switch (evt)
        {
            case KeyInputEvent k:
                var inputEvent = new InputEvent(k.ToConsoleKeyInfo());
                globalInputListener.OnInput(inputEvent);
                if (!inputEvent.Handled)
                {
                    inputEventArgs.InputEvent = inputEvent;
                    layout?.OnInput(inputEventArgs);
                }
                break;
            case MouseInputEvent m:
                ConsoleManager.MousePosition = new Position(m.X, m.Y);
                switch (m.Kind)
                {
                    case TerminalMouseKind.Down: ConsoleManager.MouseDown = true; break;
                    case TerminalMouseKind.Up: ConsoleManager.MouseDown = false; break;
                    case TerminalMouseKind.Wheel:
                        // Position was set above; dispatch the notch to the control under the pointer.
                        ConsoleManager.MouseWheel(m.Button == TerminalMouseButton.WheelUp ? -WheelLines : WheelLines);
                        break;
                        // Move/Drag only need the updated position above.
                }
                break;
            case PasteInputEvent p: 
                layout?.OnPaste(p.Text); 
                break;
            case FocusInputEvent f: 
                HasFocus = f.HasFocus; 
                break;
            // ResizeInputEvent is handled by the render loop's terminal-size check, not here.
        }
    }    
    /// <summary>
    /// Stops the UI update loop and disposes of the timer. 
    /// </summary>
    public static void Stop()
    {
        if (!isRunning) return;
        isRunning = false;
        cts.Cancel();
        (inputSource as IDisposable)?.Dispose();   // restores terminal mode for a VtInputSource
        dispatcher.Stop();
        inputThread = null;
        controls.Clear();
        ProcessMetrics.Stop();
        runCompletion.TrySetResult();
    }

    /// <summary>Moves keyboard focus to <paramref name="target"/>, clearing focus on all other registered
    /// controls (single-focus). Used by click-to-focus; runs on the UI thread.</summary>
    public static void SetFocus(IFocusable target)
    {
        Invoke(() =>
        {
            var keep = target.FocusableControl;
            foreach (var c in controls)
            {
                if (c.IsFocused && !ReferenceEquals(c, target) && !ReferenceEquals(c, keep))
                    c.IsFocused = false;
            }
            if (target.Focusable) target.IsFocused = true;
        });
    }

    /// <summary>The currently focused registered control, or <see langword="null"/> if none.</summary>
    public static IFocusable? Focused => controls.FirstOrDefault(c => c.IsFocused);

    // ---- Focus navigation -------------------------------------------------------------------------------------
    // Two tiers, both on the root layout's 2-D cell grid (Rows/Columns/this[r,c]):
    //   * Ctrl+arrows move spatially BETWEEN root cells (regions), wrapping per axis and skipping cells with no
    //     focusable; landing on a cell focuses its first focusable leaf (descending layouts, frames, composites).
    //   * Ctrl+N/P cycle WITHIN the current region — but only when that region is a multi-focusable nested layout;
    //     a single-control or composite cell is a no-op (enter/leave those with the arrows).

    /// <summary>Moves focus to the next focusable control within the current root-layout region, wrapping. Bound to
    /// <c>Ctrl+N</c> by default. A no-op unless the focused region is a multi-focusable nested layout.</summary>
    public static void FocusNext() => CycleWithinRegion(+1);

    /// <summary>Moves focus to the previous focusable control within the current region. Bound to <c>Ctrl+P</c>.</summary>
    public static void FocusPrevious() => CycleWithinRegion(-1);

    /// <summary>Moves focus one cell left/right/up/down in the root layout's 2-D grid (wraps; skips empties). Bound
    /// to <c>Ctrl+Left/Right/Up/Down</c> by default.</summary>
    public static void FocusLeft() => MoveSpatial(0, -1);
    public static void FocusRight() => MoveSpatial(0, +1);
    public static void FocusUp() => MoveSpatial(-1, 0);
    public static void FocusDown() => MoveSpatial(+1, 0);

    private static void MoveSpatial(int dRow, int dCol) => Invoke(() =>
    {
        if (layout is not { } root) return;
        int rows = root.Rows, cols = root.Columns;
        if (rows <= 0 || cols <= 0) return;

        var (r, c) = CurrentCell(root) ?? (0, 0);
        for (var step = 0; step < rows * cols; step++)   // walk in the given direction, wrapping, until a focusable cell
        {
            r = ((r + dRow) % rows + rows) % rows;
            c = ((c + dCol) % cols + cols) % cols;
            if (FirstLeaf(SafeCell(root, r, c)) is { } target) { SetFocus(target); return; }
        }
    });

    private static void CycleWithinRegion(int direction) => Invoke(() =>
    {
        if (layout is not { } root || CurrentCell(root) is not { } cell) return;
        // Only a multi-focusable region cycles; a single control / composite cell is a no-op (use the arrows there).
        if (SafeCell(root, cell.Item1, cell.Item2) is not ILayout region) return;

        var ring = LeavesIn(region).ToList();
        if (ring.Count <= 1) return;
        var current = ring.FindIndex(f => ReferenceEquals(f, Focused));
        var next = current < 0
            ? (direction > 0 ? 0 : ring.Count - 1)
            : ((current + direction) % ring.Count + ring.Count) % ring.Count;
        SetFocus(ring[next]);
    });

    // The (row, column) of the root cell whose subtree currently holds focus, or null if none.
    private static (int, int)? CurrentCell(ILayout root)
    {
        for (var r = 0; r < root.Rows; r++)
            for (var c = 0; c < root.Columns; c++)
                if (SafeCell(root, r, c)?.FocusedControl is not null) return (r, c);
        return null;
    }

    // Indexer access that tolerates an empty slot (a sparse Grid cell can throw from the underlying indexer).
    private static IFocusable? SafeCell(ILayout layout, int r, int c)
    {
        try { return layout[r, c]; } catch { return null; }
    }

    private static IFocusable? FirstLeaf(IFocusable? node) => LeavesIn(node).FirstOrDefault();

    // The focusable leaves reachable from a node, in order: descends nested layouts, frames, and composites; a leaf
    // is an interactive, laid-out Control (Focusable + HandlesInput + HasLayout).
    private static IEnumerable<IFocusable> LeavesIn(IFocusable? node)
    {
        if (node is null) yield break;
        if (node is CompositeControl composite && composite.ContentLayout is { } content)
        {
            foreach (var f in LeavesIn(content)) yield return f;
        }
        else if (node is ILayout nested)
        {
            for (var r = 0; r < nested.Rows; r++)
                for (var c = 0; c < nested.Columns; c++)
                    foreach (var f in LeavesIn(SafeCell(nested, r, c))) yield return f;
        }
        else if (node is ControlFrame frame)
        {
            foreach (var f in LeavesIn(frame.Control)) yield return f;
        }
        else if (node is Control control && control.Focusable && control.HandlesInput && control.HasLayout)
        {
            yield return control;
        }
    }

    /// <summary>
    /// Registers an application-wide hotkey handled <em>before</em> any control (so it works regardless of focus),
    /// overwriting any existing action for the same key. <see cref="HotKeys.CtrlQ"/> → <see cref="Stop"/> is
    /// registered by default. Typically called before <see cref="Start"/>; the key must match what the input
    /// decoder produces (use the <see cref="HotKeys"/> constants/helpers).
    /// </summary>
    public static void RegisterHotKey(ConsoleKeyInfo key, Action action) => Invoke(() => GlobalHotKeys[key] = action);

    /// <summary>Removes a global hotkey registered via <see cref="RegisterHotKey"/>.</summary>
    public static void UnregisterHotKey(ConsoleKeyInfo key) => Invoke(() => GlobalHotKeys.Remove(key));

    /// <summary>
    /// Marks the UI as needing a redraw on the next frame. Called whenever control content or
    /// layout changes; idle frames skip the redraw until this is set.
    /// </summary>
    public static void MarkDirty() => needsDraw = true;

    /// <summary>The UI thread dispatcher.</summary>
    public static Dispatcher Dispatcher => dispatcher;

    /// <summary>Returns <see langword="true"/> when the caller is on the UI thread (or none is running).</summary>
    public static bool CheckAccess() => dispatcher.CheckAccess();

    /// <summary>Throws when the caller is not on the UI thread.</summary>
    public static void VerifyAccess() => dispatcher.VerifyAccess();

    /// <summary>Queues an action to run on the UI thread.</summary>
    public static void Post(Action action) => dispatcher.Post(action);

    /// <summary>Runs an action on the UI thread and returns a task that completes when it finishes.</summary>
    public static Task InvokeAsync(Action action) => dispatcher.InvokeAsync(action);

    /// <summary>Runs a function on the UI thread and returns a task with its result.</summary>
    public static Task<T> InvokeAsync<T>(Func<T> func) => dispatcher.InvokeAsync(func);

    /// <summary>Runs an async delegate on the UI thread and returns a task that completes (unwrapped) when it finishes.</summary>
    public static Task InvokeAsync(Func<Task> func) => dispatcher.InvokeAsync(func);

    /// <summary>Runs an async function on the UI thread and returns a task with its (unwrapped) result.</summary>
    public static Task<T> InvokeAsync<T>(Func<Task<T>> func) => dispatcher.InvokeAsync(func);

    /// <summary>
    /// Synchronously paints a single frame: fires the <see cref="Paint"/> event so every control renders
    /// into its buffer. Intended for headless/snapshot rendering when the UI timer loop is not running.
    /// </summary>
    /// <remarks>
    /// This does not draw to a real console; callers compose the painted control buffers themselves
    /// (for example via a <see cref="ConsoleGUI.Common.DrawingContext"/>).
    /// </remarks>
    public static void PaintFrame() => _Paint?.Invoke(null, paintEventArgs);

    /// <summary>
    /// Sends a key (with optional modifiers) to a focusable..
    /// </summary>
    public static void SendInput(IFocusable target, ConsoleKeyInfo key)
        => target.FocusableControl.OnInput(new InputEventArgs(new InputEvent(key)));

    /// <summary>
    /// Sends a key (with optional modifiers) to a focusable..
    /// </summary>
    public static void SendInput(IFocusable target, ConsoleKey key, bool shift = false, bool alt = false, bool control = false)
        => target.FocusableControl.OnInput(new InputEventArgs(new InputEvent(new ConsoleKeyInfo('\0', key, shift, alt, control))));

    /// <summary>
    /// Runs once per frame on the UI thread: redraws the UI and invokes the <see cref="Paint"/> event,
    /// if the lock is available. (The lock still guards against concurrent background-thread mutation.)
    /// </summary>
    private static void OnFrame()
    {
        // Runs on the UI thread after the dispatcher queue (mutations + input) has been drained, so all
        // layout/geometry changes for this frame have already been applied on this same thread.
        if (needsDraw)
        {
            // Something changed: full draw (handles terminal resize, redraw, and draw timers).
            needsDraw = false;
            ConsoleManager.Draw();
        }
        else
        {
            // Idle: skip the full-screen scan but still detect a terminal resize cheaply
            // (AdjustBufferSize only redraws when the size actually changed).
            ConsoleManager.AdjustBufferSize();
            // Keep a self-blinking ANSI cursor ticking even with no input/animation. Cheap: emits only the cursor
            // visibility toggle when the blink phase flips (≈twice a second), not a full-screen redraw. Safe to call
            // only here (the idle branch) — on drawn frames Update handles the cursor inline.
            ConsoleManager.TickCursorBlink();
        }

        // Invoke control paint events
        paintTimer.Restart();
        _Paint?.Invoke(null, paintEventArgs);
        paintTimer.Stop();
        paintTimes[paintTimeIndex] = paintTimer.ElapsedMilliseconds;
        paintTimeIndex = (paintTimeIndex + 1) % paintTimeSamples;
    }
       
    /// <summary>
    /// Executes an action within the UI lock, ensuring thread safety for UI updates.
    /// </summary>
    /// <param name="action">The action to execute.</param>
    internal static void Invoke(Action action)
    {
        // Auto-marshal: run inline when already on the UI thread (or when no UI thread is running, e.g.
        // headless/initialization), otherwise post the mutation to the UI thread. Layout/geometry changes
        // therefore always run on the UI thread, serialized with rendering — no lock required.
        if (CheckAccess())
        {
            action();
        }
        else
        {
            dispatcher.Post(action);
        }
        // Invoke is only called when control/layout state actually changes, so request a redraw.
        needsDraw = true;
    }
    #endregion

    #region Properties
    public static ILayout Layout => layout!;

    /// <summary>
    /// The active style theme. Controls capture their default colours/decorations from it. Assigning raises
    /// <see cref="ThemeChanged"/> (on the UI thread), so every live control re-captures — i.e. assigning it is a
    /// runtime theme switch. Defaults to <see cref="DefaultStyleTheme"/>.
    /// </summary>
    public static IStyleTheme StyleTheme
    {
        get => _styleTheme;
        set => Invoke(() => { _styleTheme = value; ThemeChanged?.Invoke(null, EventArgs.Empty); });
    }

    /// <summary>
    /// The active glyph theme. Controls capture their indicator glyphs from it. Assigning raises
    /// <see cref="ThemeChanged"/> (on the UI thread), so every live control re-captures — i.e. assigning it is a
    /// runtime theme switch. Defaults to <see cref="DefaultGlyphTheme"/>.
    /// </summary>
    public static IGlyphTheme GlyphTheme
    {
        get => _glyphTheme;
        set => Invoke(() => { _glyphTheme = value; ThemeChanged?.Invoke(null, EventArgs.Empty); });
    }

    /// <summary>
    /// Convenience to set both themes at once. The work (and the <see cref="ThemeChanged"/> notification that
    /// makes live controls re-capture) is done by the <see cref="StyleTheme"/>/<see cref="GlyphTheme"/> setters.
    /// </summary>
    /// <remarks>
    /// Re-capture happens only on assignment, never on the render path, so frame-to-frame cost is unchanged.
    /// Each control re-applies the theme only to properties it has not explicitly overridden (see
    /// <c>ThemeOverrides</c>), so explicit per-control styling — including a frame's border — survives the switch.
    /// </remarks>
    public static void SetTheme(IStyleTheme styleTheme, IGlyphTheme glyphTheme)
    {
        StyleTheme = styleTheme;
        GlyphTheme = glyphTheme;
    }

    /// <summary>Applies both halves of a bundled <see cref="ITheme"/> (see <see cref="SetTheme(IStyleTheme, IGlyphTheme)"/>).</summary>
    public static void SetTheme(ITheme theme) => SetTheme(theme.Styles, theme.Glyphs);

    /// <summary>True while the UI loop is running. Background work (e.g. a Spectre progress/live loop) can poll
    /// this to exit when the UI stops.</summary>
    public static bool IsRunning => isRunning;

    /// <summary>Whether the terminal window currently has focus (DEC mode 1004). Defaults to <see langword="true"/>;
    /// updated from <see cref="FocusInputEvent"/>s once focus reporting is enabled by the raw input source.</summary>
    public static bool HasFocus { get; private set; } = true;

    /// <summary>A token that is cancelled when the UI stops. Background work started alongside the UI (e.g. a
    /// Spectre progress/live loop) should observe this so it terminates on shutdown instead of running on.</summary>
    public static CancellationToken CancellationToken => cancellationToken;
    
    public static double AveragePaintTime
    {
        get
        {
            long total = 0;
            int count = 0;
            foreach (var time in paintTimes)
            {
                if (time > 0)
                {
                    total += time;
                    count++;
                }
            }
            return count > 0 ? (double)total / count : 0;
        }
    }

    public static double AverageDrawTime => ConsoleManager.AverageDrawTime;

    public static IDictionary<IFocusable, double> AverageControlPaintTimes
    {
        get
        {
            var d = new Dictionary<IFocusable, double>();   
            foreach(var c in controlPaintTimes)
            {
                long total = 0;
                int count = 0;
                foreach (var time in c.Value)
                {
                    if (time.HasValue)
                    {
                        total += time.Value;
                        count++;
                    }
                }
                d[c.Key] = count > 0 ? (double)total / count : 0;
            }
            return d;
        }
    }

    public static IDictionary<IFocusable, long> MaxControlPaintTimes => controlPaintTimes
        .Select(kv => KeyValuePair.Create(kv.Key, kv.Value.Where(v => v.HasValue).Select(v => v!.Value).DefaultIfEmpty().Max()))
        .ToDictionary();
    #endregion

    #region Events
    /// <summary>Raised by <see cref="SetTheme"/> after the active themes change, so live controls re-apply them.
    /// Controls subscribe in their constructor and unsubscribe on <see cref="IDisposable.Dispose"/>.</summary>
    public static event EventHandler? ThemeChanged;

    private static EventHandler<PaintEventArgs>? _Paint;
    public static event EventHandler<PaintEventArgs> Paint
    {
        add
        {            
            if (value.Target is IFocusable c)
            {
                if (!controls.Contains(c))
                {
                    controls.Add(c);                 
                    controlPaintTimers[c] = new Stopwatch();
                    controlPaintTimes[c] = new long?[paintTimeSamples];                    
                    _Paint = (EventHandler<PaintEventArgs>?)Delegate.Combine(_Paint, value);
                }                
            }           
        }
        remove
        {            
            if (value.Target is IFocusable c)
            {
                _Paint ??= (EventHandler<PaintEventArgs>?)Delegate.Remove(_Paint, value);
                controls.Remove(c);
            }           
        }
    }
    #endregion

    #region Fields
    /// <summary>Lines scrolled per mouse-wheel notch.</summary>
    private const int WheelLines = 3;
    public static readonly ProcessMetrics ProcessMetrics = new ProcessMetrics(300);
    private static readonly PaintEventArgs paintEventArgs = new PaintEventArgs();
    private static readonly InputEventArgs inputEventArgs = new InputEventArgs();
    private static readonly Dispatcher dispatcher = new Dispatcher();
    private static IStyleTheme _styleTheme = new DefaultStyleTheme();
    private static IGlyphTheme _glyphTheme = new DefaultGlyphTheme();
    private static IInputSource inputSource = new ConsoleInputSource();
    private static Thread? inputThread;
    private static TaskCompletionSource runCompletion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
    private static CancellationTokenSource cts = new CancellationTokenSource();
    private static CancellationToken cancellationToken = cts.Token;
    private static int interval = 100;
    private static bool isRunning;
    private static volatile bool needsDraw = true;
    private static ILayout? layout;
    private static readonly List<IFocusable> controls = new List<IFocusable>();
    private static readonly GlobalInputListener globalInputListener = new GlobalInputListener();
    private static readonly Dictionary<ConsoleKeyInfo, Action> GlobalHotKeys = new Dictionary<ConsoleKeyInfo, Action>
    {
        { HotKeys.CtrlQ, Stop },
        // Focus navigation (the "Ctrl tier"). Ctrl+arrows move spatially between root-layout regions; Ctrl+N/P
        // cycle within the current region. Plain keys stay with the focused control; Alt is layout-specific nav.
        { HotKeys.CtrlN, FocusNext },
        { HotKeys.CtrlP, FocusPrevious },
        { HotKeys.CtrlLeft, FocusLeft },
        { HotKeys.CtrlRight, FocusRight },
        { HotKeys.CtrlUp, FocusUp },
        { HotKeys.CtrlDown, FocusDown },
    };
    private static readonly int paintTimeSamples = 60;
    private static readonly long[] paintTimes = new long[paintTimeSamples];
    private static readonly Stopwatch paintTimer = new Stopwatch();
    internal static int paintTimeIndex = 0;
    internal static readonly Dictionary<IFocusable, Stopwatch> controlPaintTimers = new();
    internal static readonly Dictionary<IFocusable, long?[]> controlPaintTimes = new();
    #endregion

    #region Types
    public class PaintEventArgs : EventArgs
    {
    }

    public class InputEventArgs : EventArgs
    {
        public InputEvent? InputEvent { get; internal set; }

        public InputEventArgs()
        {
        }

        public InputEventArgs(InputEvent? inputEvent)
        {
            InputEvent = inputEvent;
        }
    }

    public class GlobalInputListener: IInputListener
    {
        public void OnInput(InputEvent inputEvent)
        {
            if (GlobalHotKeys.TryGetValue(inputEvent.Key, out var action))
            {               
                action?.Invoke();
                inputEvent.Handled = true;
                return;                
            }           
        }
    }

    public static class HotKeys
    {
        public static ConsoleKeyInfo Ctrl(ConsoleKey key)
        {
            // For letter keys, a control character is generated. For other keys, the character is '\0'.
            char keyChar = (key >= ConsoleKey.A && key <= ConsoleKey.Z)
                ? (char)(Char.ToLower((char)key) - 96)
                : '\0';
            return new ConsoleKeyInfo(keyChar, key, false, false, true);
        }

        public static ConsoleKeyInfo Alt(ConsoleKey key)
        {
            // For letter keys, a lowercase character is generated. For other keys, the character is '\0'.
            char keyChar = (key >= ConsoleKey.A && key <= ConsoleKey.Z)
                ? Char.ToLower((char)key)
                : '\0';
            return new ConsoleKeyInfo(keyChar, key, false, true, false);
        }

        public static ConsoleKeyInfo CtrlAlt(ConsoleKey key) =>
            new('\0', key, false, true, true);

        /// <summary>The Escape key, as produced by the input decoder (KeyChar <c>\x1b</c>, no modifiers).</summary>
        public static ConsoleKeyInfo Escape = new('\x1b', ConsoleKey.Escape, false, false, false);

        /// <summary>The Tab key, as produced by the input decoder (KeyChar <c>\t</c>, no modifiers).</summary>
        public static ConsoleKeyInfo Tab = new('\t', ConsoleKey.Tab, false, false, false);

        /// <summary>Shift+Tab (back-tab), as produced by the input decoder from CSI Z (KeyChar <c>\0</c>, Shift).</summary>
        public static ConsoleKeyInfo ShiftTab = new('\0', ConsoleKey.Tab, true, false, false);

        public static ConsoleKeyInfo CtrlQ = Ctrl(ConsoleKey.Q);
        // Focus navigation (Ctrl tier): Ctrl+N/P cycle within a region; Ctrl+arrows move between regions.
        public static ConsoleKeyInfo CtrlN = Ctrl(ConsoleKey.N);
        public static ConsoleKeyInfo CtrlP = Ctrl(ConsoleKey.P);
        public static ConsoleKeyInfo CtrlLeft = Ctrl(ConsoleKey.LeftArrow);
        public static ConsoleKeyInfo CtrlRight = Ctrl(ConsoleKey.RightArrow);
        public static ConsoleKeyInfo CtrlUp = Ctrl(ConsoleKey.UpArrow);
        public static ConsoleKeyInfo CtrlDown = Ctrl(ConsoleKey.DownArrow);
        // Alt+arrows — the "Alt tier" layout navigation keys (e.g. TabPanel switches tabs on Alt+Left/Right).
        public static ConsoleKeyInfo AltUp = Alt(ConsoleKey.UpArrow);
        public static ConsoleKeyInfo AltDown = Alt(ConsoleKey.DownArrow);
        public static ConsoleKeyInfo AltLeft = Alt(ConsoleKey.LeftArrow);
        public static ConsoleKeyInfo AltRight = Alt(ConsoleKey.RightArrow);
        public static ConsoleKeyInfo CtrlAltUp = CtrlAlt(ConsoleKey.UpArrow);
    }
    #endregion
}


