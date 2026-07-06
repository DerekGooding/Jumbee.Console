namespace Jumbee.Console;

using ConsoleGUI;
using ConsoleGUI.Api;
using ConsoleGUI.Input;
using ConsoleGUI.Space;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Manages th overall UI and provides a paint event for controls to subscribe to.
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
        inputSource = input ?? DefaultInputSource(isAnsiTerminal);
        ConsoleManager.Setup();
        ConsoleManager.Resize(new Size(width, height));
        // Wrap the app's root in a UI-owned system overlay so global modals (the F1 help dialog, and any future
        // system dialog) always have a layer to show on, whatever the app's root layout is. If the root is already
        // an Overlay, reuse it. The overlay is transparent to navigation/input while no popup is shown.
        systemOverlay = layout as Overlay ?? new Overlay(layout);
        ConsoleManager.Content = systemOverlay.CControl;
        UI.layout = systemOverlay;
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
        perfHud.RegisterToggle(HotKeys.CtrlF12);
        inputThread = new Thread(InputLoop) { IsBackground = true, Name = "Jumbee.Console.Input" };
        inputThread.Start();
        RegisterSignalHandlers();        
        return runCompletion.Task;
    }

    /// <summary>
    /// Chooses the input source when the caller passed none: the raw <see cref="VtInputSource"/> (so mouse, paste,
    /// and focus reporting work) on an <em>interactive</em> ANSI terminal, otherwise the keyboard-only
    /// <see cref="ConsoleInputSource"/>. "Interactive" means neither stdin nor stdout is redirected — so a piped or
    /// redirected run (a test host, CI, `app &lt; file`, `app | tee`) keeps the safe keyboard-only source and never
    /// flips a non-terminal into raw mode. Pass an explicit <c>input:</c> to <see cref="Start"/> to override.
    /// </summary>
    internal static IInputSource DefaultInputSource(bool isAnsiTerminal)
    {
        if (isAnsiTerminal && IsInteractiveTerminal() && !NonInteractiveEnvironment())
        {
            // Guard against raw-mode setup failing on an odd host (e.g. no controlling terminal): fall back rather
            // than leaving the app with no working input.
            try { return new VtInputSource(); }
            catch { /* fall through to the keyboard-only source */ }
        }
        return new ConsoleInputSource();
    }

    // A cheap gate on top of the tty check for the common "looks like a terminal but no human is driving it" cases:
    // a CI runner (which allocates a pty yet sets CI), or an explicitly dumb terminal. It is a conservative NEGATIVE
    // signal only — false keeps the door open, true forces keyboard-only. The fully reliable positive test is an ANSI
    // query handshake (write DSR `ESC[6n` / DA `ESC[c`, wait briefly for the terminal's reply); not done here because
    // it costs startup latency and a raw-mode probe. Pass an explicit input source to Start to bypass all of this.
    private static bool NonInteractiveEnvironment()
    {
        try
        {
            if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("CI"))) return true;   // GitHub/GitLab/Circle/…
            return Environment.GetEnvironmentVariable("TERM") is "dumb";
        }
        catch { return false; }
    }

    /// <summary><see langword="true"/> when both console input and output are real terminals (not redirected/piped),
    /// so it is safe to put the terminal into raw VT input mode. This is what <see cref="Start"/> uses to decide
    /// whether to default to a mouse-capable <see cref="VtInputSource"/> when no <c>input</c> is supplied.</summary>
    public static bool IsInteractiveTerminal()
    {
        try { return !Console.IsInputRedirected && !Console.IsOutputRedirected; }
        catch { return false; }
    }

    // Safety hatch: catch external stop signals so a `kill`/`pkill` from another terminal (or the terminal window
    // closing) ALWAYS restores the terminal and exits — even when the UI/input pipeline is wedged and no in-app
    // quit (Ctrl+Q) can fire. Raw mode means Ctrl+C arrives as a byte (it reaches the app/shell, not us), so these
    // handlers respond only to actual signals, not interactive keys.
    private static void RegisterSignalHandlers()
    {
        signalRegistrations = [];
        foreach (var signal in new[] { PosixSignal.SIGTERM, PosixSignal.SIGINT, PosixSignal.SIGQUIT, PosixSignal.SIGHUP })
        {
            try { signalRegistrations.Add(PosixSignalRegistration.Create(signal, OnSignal)); }
            catch (Exception) { /* signal not supported on this platform (e.g. SIGHUP/SIGQUIT on Windows) */ }
        }
    }

    private static void OnSignal(PosixSignalContext context)
    {
        // Restore the terminal synchronously (best effort) so it happens before the process terminates, then let
        // the signal's default action stop us — we do NOT Cancel, so exit is guaranteed even if the loop is stuck.
        isRunning = false;
        try { (inputSource as IDisposable)?.Dispose(); } catch { /* best effort */ }
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
                // Latch the pressed button so the click handlers that fire on the matching Up (OnClick/OnDoubleClick,
                // dispatched synchronously when MouseDown flips to false) can tell a right-click from a left-click.
                if (m.Kind == TerminalMouseKind.Down) MouseButton = m.Button;
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
        (inputSource as IDisposable)?.Dispose();   // restores terminal mode for a VtInputSource; helps unblock a reader
        // Wait for the input reader thread to actually exit before returning. It reads the static `inputSource`, so a
        // reader left running from a previous Start would otherwise keep consuming — and reorder — a later run's input
        // (two consumers on one queue). It is a background thread and never the caller, so this join is safe/bounded.
        var reader = inputThread;
        inputThread = null;
        if (reader is not null && reader != Thread.CurrentThread) reader.Join(1000);
        dispatcher.Stop();
        if (signalRegistrations is not null)   // not disposed from a signal callback, so this never self-deadlocks
        {
            foreach (var r in signalRegistrations) r.Dispose();
            signalRegistrations = null;
        }
        controls.Clear();
        MouseButton = TerminalMouseButton.None;   // clear transient input state so it can't leak into a later session
        ProcessMetrics.Stop();
        runCompletion.TrySetResult();
    }

    /// <summary>Moves keyboard focus to <paramref name="target"/>, clearing focus on all other registered
    /// controls (single-focus). Used by click-to-focus; runs on the UI thread.</summary>
    public static void SetFocus(IFocusable target)
    {
        Invoke(() =>
        {
            // Resolve a composite's inner child up to the composite (the navigable focus unit); the composite then
            // delegates focus back to the appropriate child via Control_OnFocus. Click-to-focus and keyboard
            // navigation therefore agree on which control is "focused".
            if (target is Control tc) target = tc.FocusRoot;
            var keep = target.FocusableControl;
            foreach (var c in controls)
            {
                // Don't clear controls in the target's own subtree (a composite keeps its delegated child focused
                // when it is (re)focused); clear every other focused control for single-focus.
                if (c.IsFocused && !ReferenceEquals(c, target) && !ReferenceEquals(c, keep)
                    && !(c is Control cc && ReferenceEquals(cc.FocusRoot, target)))
                    c.IsFocused = false;
            }
            if (target.Focusable) target.IsFocused = true;
        });
    }

    /// <summary>The currently focused registered control, or <see langword="null"/> if none.</summary>
    public static IFocusable? Focused => controls.FirstOrDefault(c => c.IsFocused);

    /// <summary>
    /// Opens the global help dialog — a modal with one tab per control (compiled from every control's
    /// <see cref="Control.GetHelpInfo"/>, deduplicated by <see cref="HelpInfo.Name"/>), the focused control's tab
    /// shown first. Bound to <see cref="HotKeys.F1"/> by default; pressing it again closes the dialog. A no-op when
    /// no control supplies help. The dialog is shown on the UI-owned system overlay (see <see cref="Start"/>).
    /// </summary>
    public static void ShowHelp() => Invoke(() =>
    {
        if (systemOverlay is null) return;
        if (systemOverlay.Top is HelpControl) { systemOverlay.Hide(); return; }   // F1 toggles the dialog

        var infos = CompileHelp();
        if (infos.Count <= 1) return;   // only the built-in General entry — no control supplied help

        // Open on the focused control's tab (resolve an inner composite child up to its owning unit, then match by
        // name since that tab may be a shared/deduplicated entry).
        var focusName = Focused is Control fc ? (fc.FocusRoot.CompileHelp() ?? fc.CompileHelp())?.Name : null;
        var start = 0;
        if (focusName is not null)
            for (var i = 0; i < infos.Count; i++)
                if (infos[i].Name == focusName) { start = i; break; }

        systemOverlay.ShowModal(new HelpControl(infos, HideHelp, start));
    });

    /// <summary>
    /// Compiles the help shown by <see cref="ShowHelp"/>: a built-in "General" entry (global keys) followed by each
    /// registered control's <see cref="Control.GetHelpInfo"/> (modified by its <see cref="Control.OnHelp"/>
    /// handlers), deduplicated by <see cref="HelpInfo.Name"/>. Exposed so an app can present its own help UI.
    /// </summary>
    public static IReadOnlyList<HelpInfo> CompileHelp()
    {
        var infos = new List<HelpInfo>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        // A built-in entry for the global keys, always first.
        var general = new HelpInfo("General", text: "Global keys, available anywhere in the app.")
            .WithKey("F1", "Show / hide this help")
            .WithKey("Ctrl+Q", "Quit")
            .WithKey("Ctrl+← ↑ → ↓", "Move focus between regions")
            .WithKey("Ctrl+N / Ctrl+P", "Next / previous control in a region");
        infos.Add(general);
        seen.Add(general.Name);
        foreach (var f in controls.ToArray())
            if (f is Control c && c.CompileHelp() is { Name.Length: > 0 } info && seen.Add(info.Name))
                infos.Add(info);
        return infos;
    }

    /// <summary>Closes the global help dialog if it is open.</summary>
    public static void HideHelp() => Invoke(() => { if (systemOverlay?.Top is HelpControl) systemOverlay.Hide(); });

    // ---- Focus navigation -------------------------------------------------------------------------------------
    // Two tiers, both on the root layout's 2-D cell grid (Rows/Columns/this[r,c]):
    //   * Ctrl+arrows move spatially BETWEEN root cells (regions), wrapping per axis and skipping cells with no
    //     focusable; landing on a cell focuses its first focusable leaf (descending layouts, frames, composites).
    //   * Ctrl+N/P cycle WITHIN the current region — but only when that region is a multi-focusable nested layout;
    //     a single-control or composite cell is a no-op (enter/leave those with the arrows).

    /// <summary>Moves focus to the next focusable control within the current root-layout region, wrapping. Bound to
    /// <c>Ctrl+N</c> by default. A no-op unless the focused region is a multi-focusable nested layout.</summary>
    public static void FocusNext() => CycleFocusWithinRegion(+1);

    /// <summary>Moves focus to the previous focusable control within the current region. Bound to <c>Ctrl+P</c>.</summary>
    public static void FocusPrevious() => CycleFocusWithinRegion(-1);

    /// <summary>Moves focus one cell left/right/up/down in the root layout's 2-D grid (wraps; skips empties). Bound
    /// to <c>Ctrl+Left/Right/Up/Down</c> by default.</summary>
    public static void FocusLeft() => MoveSpatialFocus(0, -1);
    public static void FocusRight() => MoveSpatialFocus(0, +1);
    public static void FocusUp() => MoveSpatialFocus(-1, 0);
    public static void FocusDown() => MoveSpatialFocus(+1, 0);

    private static void MoveSpatialFocus(int dRow, int dCol) => Invoke(() =>
    {
        if (layout?.SpatialTarget(dRow, dCol) is { } target) SetFocus(target);
    });

    private static void CycleFocusWithinRegion(int direction) => Invoke(() =>
    {
        if (layout?.RegionCycleTarget(direction, Focused) is { } target) SetFocus(target);
    });

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

    /// <summary>
    /// Queues <paramref name="action"/> to run on the UI thread on a later turn of the frame loop, and returns
    /// immediately (never blocks, never runs inline). Unlike <see cref="Invoke"/>, it <em>always</em> defers — even
    /// when the caller is already on the UI thread — so the action runs after the current work/layout/paint settles
    /// (work posted while the queue is draining waits for the next frame; see <see cref="Dispatcher"/>).
    /// </summary>
    /// <remarks>
    /// Use this — rather than <see cref="Invoke"/> — when you specifically want to defer: to break re-entrancy (e.g.
    /// run something <em>after</em> the current input/layout pass), or for a self-feeding pump that must not starve
    /// rendering. It is the low-level primitive: it does NOT request a redraw, so if the action changes visual state
    /// it must invalidate itself (or call <see cref="MarkDirty"/>). For "run on the UI thread, now if I already am,"
    /// use <see cref="Invoke"/> instead.
    /// </remarks>
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
        // Bracket the draw/paint cycle to measure its real cost directly: high-resolution wall time (immune to the
        // ~15 ms coarseness of process CPU time) and the managed-heap allocation it caused. Also track the frame
        // period for UI-thread utilisation. ProcessMetrics reads these as peaks so a burst (e.g. a paste) shows.
        long frameStart = Stopwatch.GetTimestamp();
        double periodMs = lastFrameStart != 0 ? (frameStart - lastFrameStart) * 1000.0 / Stopwatch.Frequency : 0;
        lastFrameStart = frameStart;
        long allocBefore = GC.GetTotalAllocatedBytes();
        frameTimer.Restart();

        bool drew = false;
        double dirtyFraction = 0;
        try
        {
            // 1. Detect a terminal resize BEFORE painting, so controls re-lay-out at the new size and repaint into
            //    correctly-sized buffers this same frame (a resize marks the whole surface dirty via Initialize).
            //    Runs every frame (cheap when the size is unchanged); also ticks the legacy software cursor blink.
            ConsoleManager.AdjustBufferSize();

            // A redraw requested BEFORE this frame's paint (from input, a timer, or a structural change drained off
            // the dispatcher queue just now) should be fully satisfied this frame. Captured here so it can be told
            // apart from a redraw requested DURING the paint below — e.g. a control's throttled self-refresh
            // (StatusBar/PerfHud) invalidating from its own paint handler, whose actual repaint bubbles next frame.
            bool redrawRequestedBeforePaint = needsDraw;

            // 2. Paint: controls render into their own buffers and report their damaged screen rects into the
            //    ConsoleManager dirty accumulator. This MUST run before compositing so the composite reads fresh
            //    buffers, not last frame's (or, on the first frame after startup/resize, empty ones) — otherwise
            //    the newly-painted panes show blank until the next full redraw.
            paintTimer.Restart();
            _Paint?.Invoke(null, paintEventArgs);
            paintTimer.Stop();
            // Fractional milliseconds: paints are typically sub-millisecond, which ElapsedMilliseconds truncates to 0.
            paintTimes[paintTimeIndex] = paintTimer.Elapsed.TotalMilliseconds;
            paintTimeIndex = (paintTimeIndex + 1) % paintTimeSamples;

            // 3. Composite the damaged region(s): the whole screen on a full-dirty (startup/resize), else just the
            //    dirty rects reported by the paints above. An idle frame (nothing dirtied) skips the scan and only
            //    keeps a self-blinking ANSI cursor ticking.
            if (needsDraw || ConsoleManager.HasDirty)
            {
                // Safety net: a redraw was requested before this frame's paint, yet the paint localized no damage
                // (HasDirty is false) and nothing marked the surface fully dirty — so the change came from a source
                // that can't be scoped to a rect (e.g. a self-drawing popup opening a submenu). Re-composite
                // everything rather than leave a stale region. A needsDraw raised DURING the paint (a deferred
                // self-refresh) is NOT promoted — its real partial redraw lands next frame — which is what kept the
                // status-bar tick from repainting the whole screen.
                if (redrawRequestedBeforePaint && !ConsoleManager.HasDirty) ConsoleManager.MarkFullDirty();
                needsDraw = false;
                drew = true;
                ConsoleManager.Draw();
                var sz = ConsoleManager.BufferSize;
                long area = (long)sz.Width * sz.Height;
                if (area > 0) dirtyFraction = ConsoleManager.LastFrameDirtyCells / (double)area;
            }
            else
            {
                ConsoleManager.TickCursorBlink();
            }
        }
        finally
        {
            // Record the frame even if the draw/paint threw, so the metrics keep working (and exceptions/s surfaces
            // a per-frame throw) instead of silently freezing at 0.
            frameTimer.Stop();
            ProcessMetrics.RecordFrame(frameTimer.Elapsed.TotalMilliseconds, periodMs, GC.GetTotalAllocatedBytes() - allocBefore, drew, dirtyFraction);
        }
    }
       
    /// <summary>
    /// Runs <paramref name="action"/> on the UI thread, marshaling automatically: <em>inline and immediately</em>
    /// when the caller is already on the UI thread (or no UI thread is running, e.g. headless/initialization),
    /// otherwise posted to the dispatcher queue. Then requests a redraw. This is the primary, default way to mutate
    /// control/layout state from anywhere — the change always ends up serialized on the UI thread with rendering, so
    /// no lock is needed.
    /// </summary>
    /// <remarks>
    /// Does NOT block: off-thread it is fire-and-forget (the action runs later on the UI thread) — it does not wait
    /// for completion or surface the action's exception to the caller. That is unlike the blocking, WPF-style
    /// <see cref="Dispatcher.Invoke(Action)"/>; if you need to wait for the result use <see cref="InvokeAsync(Action)"/>.
    /// Differs from <see cref="Post"/> in two ways: (1) it runs inline when already on the UI thread instead of
    /// always deferring to a later frame, and (2) it requests a redraw afterwards. Prefer this for state changes;
    /// reach for <see cref="Post"/> only when you deliberately want to defer to a later frame.
    /// </remarks>
    /// <param name="action">The action to execute on the UI thread.</param>
    public static void Invoke(Action action)
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
    /// The root overlay host, available once <see cref="Start"/> has run. <see cref="Start"/> wraps the app's root
    /// in this overlay (reusing it if the root already is an <see cref="Overlay"/>), so there is always a layer for
    /// pop-ups (dropdowns, menus, autocomplete, modals). Controls that show pop-ups — <see cref="Select"/>,
    /// <see cref="MenuBar"/>, <see cref="ContextMenu"/>, <see cref="Autocomplete"/> — show into this automatically,
    /// so there is no per-control overlay to wire up. <see langword="null"/> before <see cref="Start"/>.
    /// </summary>
    /// <remarks>Normally you never set this — <see cref="Start"/> does. The setter exists for advanced hosting (and
    /// tests) that need to designate the overlay pop-ups show into without going through <see cref="Start"/>; the
    /// value must be the overlay that is actually being rendered as the root, or pop-ups won't be visible.</remarks>
    public static Overlay? Overlay
    {
        get => systemOverlay;
        set => systemOverlay = value;
    }

    /// <summary>The mouse button of the most recent press, latched until the next press. A control's
    /// <c>OnClick</c>/<c>OnDoubleClick</c> reads this to distinguish a right-click (e.g. to open a context menu)
    /// from a left-click — the dispatch itself carries only a position, not a button.</summary>
    public static TerminalMouseButton MouseButton { get; private set; }

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
            double total = 0;
            int count = 0;
            foreach (var time in paintTimes)
            {
                if (time > 0)
                {
                    total += time;
                    count++;
                }
            }
            return count > 0 ? total / count : 0;
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
                double total = 0;
                int count = 0;
                foreach (var time in c.Value)
                {
                    if (time.HasValue)
                    {
                        total += time.Value;
                        count++;
                    }
                }
                d[c.Key] = count > 0 ? total / count : 0;
            }
            return d;
        }
    }

    public static IDictionary<IFocusable, double> MaxControlPaintTimes => controlPaintTimes
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
                // Track the control (for per-control paint timing / focus) once, but ALWAYS combine the handler:
                // a control may legitimately add more than one Paint handler (e.g. PerfHud adds base Control.OnPaint
                // and its own OnHudPaint). Gating the combine on "not already tracked" silently dropped the second,
                // so such a control's extra handler never fired.
                if (!controls.Contains(c))
                {
                    controls.Add(c);
                    controlPaintTimers[c] = new Stopwatch();
                    controlPaintTimes[c] = new double?[paintTimeSamples];
                }
                _Paint = (EventHandler<PaintEventArgs>?)Delegate.Combine(_Paint, value);
            }
        }
        remove
        {
            if (value.Target is IFocusable c)
            {
                _Paint = (EventHandler<PaintEventArgs>?)Delegate.Remove(_Paint, value);
                // Untrack the control only once none of its Paint handlers remain (a control may have added several).
                bool stillSubscribed = _Paint is not null && _Paint.GetInvocationList().Any(d => ReferenceEquals(d.Target, c));
                if (!stillSubscribed)
                {
                    controls.Remove(c);
                    controlPaintTimers.Remove(c);
                    controlPaintTimes.Remove(c);
                }
            }
        }
    }
    #endregion

    #region Fields
    /// <summary>Lines scrolled per mouse-wheel notch.</summary>
    private const int WheelLines = 3;
    public static readonly ProcessMetrics ProcessMetrics = new ProcessMetrics();
    private static readonly PaintEventArgs paintEventArgs = new PaintEventArgs();
    private static readonly InputEventArgs inputEventArgs = new InputEventArgs();
    private static readonly Dispatcher dispatcher = new Dispatcher();
    private static IStyleTheme _styleTheme = new DefaultStyleTheme();
    private static IGlyphTheme _glyphTheme = new DefaultGlyphTheme();
    private static IInputSource inputSource = new ConsoleInputSource();
    private static Thread? inputThread;
    private static List<PosixSignalRegistration>? signalRegistrations;
    private static TaskCompletionSource runCompletion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
    private static CancellationTokenSource cts = new CancellationTokenSource();
    private static CancellationToken cancellationToken = cts.Token;
    private static int interval = 100;
    private static bool isRunning;
    private static volatile bool needsDraw = true;
    private static ILayout? layout;
    // UI-owned overlay wrapping the app root; hosts global modals (the F1 help dialog). Set in Start.
    private static Overlay? systemOverlay;
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
        // Global help dialog (toggles open/closed).
        { HotKeys.F1, ShowHelp }       

    };
    private static readonly int paintTimeSamples = 60;
    private static readonly double[] paintTimes = new double[paintTimeSamples];
    private static readonly Stopwatch paintTimer = new Stopwatch();
    // Measures the whole draw/paint cycle each frame (fed to ProcessMetrics.RecordFrame); lastFrameStart tracks the
    // frame period for UI-thread utilisation.
    private static readonly Stopwatch frameTimer = new Stopwatch();
    private static long lastFrameStart;
    internal static int paintTimeIndex = 0;
    internal static readonly Dictionary<IFocusable, Stopwatch> controlPaintTimers = new();
    // Fractional milliseconds per control per frame (null when the control didn't repaint that frame). Fractional,
    // not whole ms — most controls (buttons, tab items, labels) repaint in well under 1 ms and would otherwise all
    // record 0, biasing every per-control average/peak to zero.
    internal static readonly Dictionary<IFocusable, double?[]> controlPaintTimes = new();
    private static readonly PerfHud perfHud = new PerfHud();
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

        /// <summary>The F1 key, as produced by the input decoder (SS3 <c>ESC O P</c> → KeyChar <c>\0</c>, no modifiers).</summary>
        public static ConsoleKeyInfo F1 = new('\0', ConsoleKey.F1, false, false, false);

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
        public static ConsoleKeyInfo CtrlF12 = Ctrl(ConsoleKey.F12);
        // Alt+arrows — the "Alt tier" layout navigation keys (e.g. TabPanel switches tabs on Alt+Left/Right).
        public static ConsoleKeyInfo AltUp = Alt(ConsoleKey.UpArrow);
        public static ConsoleKeyInfo AltDown = Alt(ConsoleKey.DownArrow);
        public static ConsoleKeyInfo AltLeft = Alt(ConsoleKey.LeftArrow);
        public static ConsoleKeyInfo AltRight = Alt(ConsoleKey.RightArrow);
        public static ConsoleKeyInfo CtrlAltUp = CtrlAlt(ConsoleKey.UpArrow);
    }
    #endregion
}


