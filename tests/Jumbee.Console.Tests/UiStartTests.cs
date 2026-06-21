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
