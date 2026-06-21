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
    public static Task Start(ILayout layout, int width = 110, int height = 25, int paintInterval = 100, bool isTrueColorTerminal = true, IConsole? console = null, IInputSource? input = null)
    {
        if (isRunning) return runCompletion.Task;
        ProcessMetrics.Start();
        if (console != null)
        {
            ConsoleManager.Console = console;
        }
        else if (!isTrueColorTerminal)
        {
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
        // Start the single UI thread (drains the dispatcher queue, then renders, each frame).
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
            case KeyInputEvent k: DispatchKey(k.ToConsoleKeyInfo()); break;
            case MouseInputEvent m: DispatchMouse(m); break;
            case PasteInputEvent p: layout?.OnPaste(p.Text); break;
            case FocusInputEvent f: HasFocus = f.HasFocus; break;
            // ResizeInputEvent is handled by the render loop's terminal-size check, not here.
        }
    }

    /// <summary>Dispatches a key: global hotkeys first, then the focused control.</summary>
    private static void DispatchKey(ConsoleKeyInfo key)
    {
        var inputEvent = new InputEvent(key);
        globalInputListener.OnInput(inputEvent);
        if (!inputEvent.Handled)
        {
            inputEventArgs.InputEvent = inputEvent;
            layout?.OnInput(inputEventArgs);
        }
    }

    /// <summary>Feeds a mouse event into ConsoleGUI's mouse routing (hit-test + enter/leave/move/down/up).</summary>
    private static void DispatchMouse(MouseInputEvent m)
    {
        ConsoleManager.MousePosition = new Position(m.X, m.Y);
        switch (m.Kind)
        {
            case TerminalMouseKind.Down: ConsoleManager.MouseDown = true; break;
            case TerminalMouseKind.Up: ConsoleManager.MouseDown = false; break;
            // Move/Drag only need the updated position above. Wheel has no ConsoleGUI equivalent yet (deferred).
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
        dispatcher.Stop();
        inputThread = null;
        controls.Clear();
        ProcessMetrics.Stop();
        runCompletion.TrySetResult();
    }

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
    public static readonly ProcessMetrics ProcessMetrics = new ProcessMetrics(300);
    private static readonly PaintEventArgs paintEventArgs = new PaintEventArgs();
    private static readonly InputEventArgs inputEventArgs = new InputEventArgs();
    private static readonly Dispatcher dispatcher = new Dispatcher();
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
        { HotKeys.CtrlQ, Stop }
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

        public static ConsoleKeyInfo CtrlQ = Ctrl(ConsoleKey.Q);
        public static ConsoleKeyInfo CtrlN = Ctrl(ConsoleKey.N);
        public static ConsoleKeyInfo AltUp = Alt(ConsoleKey.UpArrow);
        public static ConsoleKeyInfo AltDown = Alt(ConsoleKey.DownArrow);
        public static ConsoleKeyInfo CtrlAltUp = CtrlAlt(ConsoleKey.UpArrow);
    }
    #endregion
}


