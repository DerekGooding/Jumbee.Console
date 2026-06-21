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
        private readonly ConcurrentQueue<ConsoleKeyInfo> _keys = new();
        public void Push(ConsoleKey key) => _keys.Enqueue(new ConsoleKeyInfo('\0', key, false, false, false));
        public bool TryReadKey(out ConsoleKeyInfo key) => _keys.TryDequeue(out key);
    }

    private sealed class KeyRecorderControl : Control
    {
        public readonly ConcurrentQueue<ConsoleKey> Received = new();
        public override bool HandlesInput => true;
        protected override void Render() { }
        protected override void OnInput(InputEvent e)
        {
            Received.Enqueue(e.Key.Key);
            e.Handled = true;
        }
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
