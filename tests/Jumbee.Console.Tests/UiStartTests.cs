namespace Jumbee.Console.Tests;

using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using ConsoleGUI.Input;
using ConsoleGUI.Space;

using Jumbee.Console;

using Xunit;

public class UiStartTests
{
    // Begin each test from a fully stopped UI (shared process-wide static state across sequential tests).
    public UiStartTests() => UiTestHarness.EnsureStopped();

    private sealed class FakeInputSource : IInputSource
    {
        private readonly ConcurrentQueue<TerminalInputEvent> _events = new();
        public void Push(ConsoleKey key) => _events.Enqueue(new KeyInputEvent(key, '\0', TerminalModifiers.None));
        public void Push(TerminalInputEvent evt) => _events.Enqueue(evt);
        public bool TryRead(out TerminalInputEvent? evt)
        {
            if (_events.TryDequeue(out var e)) { evt = e; return true; }
            evt = null;
            return false;
        }
    }

    // Stamps an 'X' at (0,0) of its buffer each render — used to observe the render output.
    private sealed class StampControl : Control
    {
        public override bool HandlesInput => false;
        protected override void Render()
        {
            if (ActualWidth > 0 && ActualHeight > 0)
                consoleBuffer.Write(new Position(0, 0), new ConsoleGUI.Data.Character('X'));
        }
    }

    // Marks (0,0) as a steady-block cursor cell (style 2) with known fg/bg, to observe legacy software-cursor rendering.
    private sealed class CursorStampControl : Control
    {
        public override bool HandlesInput => false;
        protected override void Render()
        {
            if (ActualWidth <= 0 || ActualHeight <= 0) return;
            var deco = ConsoleGUI.Data.CursorEncoding.EncodeStyle(ConsoleGUI.Data.Decoration.None, 2); // steady block
            consoleBuffer.Write(new Position(0, 0), new ConsoleGUI.Data.Character(
                'Z', new ConsoleGUI.Data.Color(255, 0, 0), new ConsoleGUI.Data.Color(0, 0, 255), deco, isCursor: true));
        }
    }

    private sealed class KeyRecorderControl : Control
    {
        public readonly ConcurrentQueue<ConsoleKey> Received = new();
        public readonly ConcurrentQueue<string> Pasted = new();
        public override bool HandlesInput => true;
        protected override void Render() { }
        protected override void OnInput(InputEvent e)
        {
            Received.Enqueue(e.Key.Key);
            e.Handled = true;
        }
        public override void OnPaste(string text) => Pasted.Enqueue(text);
    }

    private static bool WaitUntil(Func<bool> condition, int timeoutMs)
    {
        var sw = Stopwatch.StartNew();
        while (sw.ElapsedMilliseconds < timeoutMs)
        {
            if (condition()) return true;
            Thread.Sleep(10);
        }
        return condition();
    }

    [Fact]
    public void Start_RoutesInjectedInput_ToFocusedControl_AndStopCompletesRunTask()
    {
        var originalOut = Console.Out;
        Console.SetOut(TextWriter.Null);   // swallow ConsoleManager's ANSI output

        var control = new KeyRecorderControl();
        var grid = new Grid([5], [20], [[control]]);
        var input = new FakeInputSource();
        Task run;

        try
        {
            control.IsFocused = true;

            run = UI.Start(
                grid, width: 20, height: 5, paintInterval: 20,
                console: new ConsoleBuffer { Size = new Size(20, 5) },
                input: input);

            input.Push(ConsoleKey.DownArrow);
            input.Push(ConsoleKey.UpArrow);
            input.Push(ConsoleKey.Enter);

            Assert.True(
                WaitUntil(() => control.Received.Count >= 3, 3000),
                $"Expected 3 keys routed, got {control.Received.Count}.");
            Assert.Equal(
                new[] { ConsoleKey.DownArrow, ConsoleKey.UpArrow, ConsoleKey.Enter },
                control.Received.ToArray());
        }
        finally
        {
            UI.Stop();
            Console.SetOut(originalOut);
        }

        Assert.True(run.Wait(2000), "Stop() should complete the run task.");
    }

    [Fact]
    public void Start_NonAnsi_RendersThroughIConsoleWrite()
    {
        var originalOut = Console.Out;
        Console.SetOut(TextWriter.Null);

        var screen = new ConsoleBuffer { Size = new Size(10, 3) };
        var grid = new Grid([3], [10], [[new StampControl()]]);
        Task run;

        try
        {
            // isAnsiTerminal: false routes rendering through IConsole.Write instead of emitting ANSI.
            run = UI.Start(grid, width: 10, height: 3, paintInterval: 20, isAnsiTerminal: false, console: screen);

            Assert.True(
                WaitUntil(() => screen[0, 0].Content == 'X', 3000),
                "Non-ANSI rendering should write the cell through IConsole.Write into the screen buffer.");
        }
        finally
        {
            UI.Stop();
            Console.SetOut(originalOut);
        }

        Assert.True(run.Wait(2000), "Stop() should complete the run task.");
    }

    [Fact]
    public void Start_NonAnsi_RendersSoftwareCursor_BlockInverted()
    {
        var originalOut = Console.Out;
        Console.SetOut(TextWriter.Null);

        var screen = new ConsoleBuffer { Size = new Size(10, 3) };
        var grid = new Grid([3], [10], [[new CursorStampControl()]]);
        Task run;

        try
        {
            run = UI.Start(grid, width: 10, height: 3, paintInterval: 20, isAnsiTerminal: false, console: screen);

            Assert.True(WaitUntil(() => screen[0, 0].Content == 'Z', 3000), "software cursor cell should be rendered");
            // A steady block cursor renders the cell with fg/bg inverted (hardware cursor unused).
            Assert.True(screen[0, 0].Foreground == new ConsoleGUI.Data.Color(0, 0, 255), "fg should be the source bg");
            Assert.True(screen[0, 0].Background == new ConsoleGUI.Data.Color(255, 0, 0), "bg should be the source fg");
        }
        finally
        {
            UI.Stop();
            Console.SetOut(originalOut);
        }

        Assert.True(run.Wait(2000), "Stop() should complete the run task.");
    }

    [Fact]
    public void Start_RoutesPasteEvent_ToFocusedControl()
    {
        var originalOut = Console.Out;
        Console.SetOut(TextWriter.Null);

        var control = new KeyRecorderControl();
        var grid = new Grid([5], [20], [[control]]);
        var input = new FakeInputSource();
        Task run;

        try
        {
            control.IsFocused = true;
            run = UI.Start(grid, width: 20, height: 5, paintInterval: 20,
                console: new ConsoleBuffer { Size = new Size(20, 5) }, input: input);

            input.Push(new PasteInputEvent("hello\nworld"));

            Assert.True(
                WaitUntil(() => control.Pasted.Count >= 1, 3000),
                "Expected the paste to reach the focused control.");
            Assert.Equal("hello\nworld", control.Pasted.ToArray()[0]);
        }
        finally
        {
            UI.Stop();
            Console.SetOut(originalOut);
        }

        Assert.True(run.Wait(2000), "Stop() should complete the run task.");
    }

    [Fact]
    public void Start_WrapsPlainRootInOverlay_AndOverlayControlsDefaultToIt()
    {
        var originalOut = Console.Out;
        Console.SetOut(TextWriter.Null);

        var select = new Select("Alpha", "Beta", "Gamma");   // note: no explicit .Overlay set
        var grid = new Grid([3], [20], [[select]]);          // a plain layout, not an Overlay
        Task run;

        try
        {
            run = UI.Start(grid, width: 20, height: 8, paintInterval: 20,
                console: new ConsoleBuffer { Size = new Size(20, 8) }, input: new FakeInputSource());

            // Start wraps a non-Overlay root in a UI-owned overlay, exposed as UI.Overlay.
            Assert.NotNull(UI.Overlay);
            Assert.Null(UI.Overlay!.Top);   // nothing shown yet

            // Opening the dropdown with no explicit host must fall back to the ambient UI.Overlay.
            UI.Invoke(() => select.Open());
            Assert.True(
                WaitUntil(() => UI.Overlay!.Top is not null, 2000),
                "A Select with no explicit Overlay should open its dropdown into the ambient UI.Overlay.");
        }
        finally
        {
            UI.Stop();
            Console.SetOut(originalOut);
        }

        Assert.True(run.Wait(2000), "Stop() should complete the run task.");
    }

    [Fact]
    public void Stop_InvokedOnUiThread_DoesNotSelfJoinHang_AndCompletesRunTask()
    {
        var originalOut = Console.Out;
        Console.SetOut(TextWriter.Null);

        var control = new KeyRecorderControl();
        var grid = new Grid([5], [20], [[control]]);
        Task run;

        try
        {
            run = UI.Start(
                grid, width: 20, height: 5, paintInterval: 20,
                console: new ConsoleBuffer { Size = new Size(20, 5) },
                input: new FakeInputSource());

            // Simulate the Ctrl-Q hotkey path: Stop runs on the UI thread. The dispatcher must not try to
            // Join its own thread (which would block for the full timeout).
            UI.Post(() => UI.Stop());

            Assert.True(run.Wait(2000), "Stop() from the UI thread should complete the run task promptly.");
            Assert.True(WaitUntil(() => !UI.Dispatcher.IsRunning, 2000), "Dispatcher should have stopped.");
            Assert.False(UI.IsRunning);
        }
        finally
        {
            UI.Stop();
            Console.SetOut(originalOut);
        }
    }
}
