namespace Jumbee.Console.Tests;

using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using ConsoleGUI.Space;

using Jumbee.Console;

using Xunit;

public class TimingMetricsTests
{
    public TimingMetricsTests() => UiTestHarness.EnsureStopped();

    // Repaints itself every frame, so every frame is a "dirty" frame that runs a full draw — accumulating draw and
    // paint timer samples.
    private sealed class AnimatingControl : Control
    {
        public override bool HandlesInput => false;
        protected override void Render()
        {
            if (ActualWidth > 0 && ActualHeight > 0)
                consoleBuffer.Write(new Position(0, 0), new ConsoleGUI.Data.Character('.'));
            Invalidate();
        }
    }

    // The draw/paint timers store fractional milliseconds (Elapsed.TotalMilliseconds), so sub-millisecond frames —
    // which the retained/dirty-rect renderer produces — register as small non-zero values instead of truncating to
    // 0 (the old ElapsedMilliseconds behavior that made a µs perf HUD read a flat 0).
    [Fact]
    public void DrawAndPaintTimers_RecordSubMillisecondFrames_AsNonZero()
    {
        var originalOut = Console.Out;
        Console.SetOut(TextWriter.Null);

        var screen = new ConsoleBuffer { Size = new Size(20, 5) };
        var grid = new Grid([5], [20], [[new AnimatingControl()]]);
        Task run;
        try
        {
            run = UI.Start(grid, width: 20, height: 5, paintInterval: 10, isAnsiTerminal: false, console: screen);

            Assert.True(
                WaitUntil(() => UI.AverageDrawTime > 0 && UI.AveragePaintTime > 0, 3000),
                $"timers should record non-zero fractional ms (draw={UI.AverageDrawTime}, paint={UI.AveragePaintTime})");
        }
        finally
        {
            UI.Stop();
            Console.SetOut(originalOut);
        }

        Assert.True(run.Wait(2000), "Stop() should complete the run task.");
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
}
