namespace Jumbee.Console.Tests;

using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using ConsoleGUI.Space;

using Jumbee.Console;

using Xunit;

public class ConcurrencyTests
{
    // Begin each test from a fully stopped UI (shared process-wide static state across sequential tests).
    public ConcurrencyTests() => UiTestHarness.EnsureStopped();

    /// <summary>
    /// Phase C: collection mutations marshal onto the UI thread (via <c>UI.Invoke</c>), so <c>ListBox</c> can
    /// use a plain <see cref="System.Collections.Generic.Dictionary{TKey,TValue}"/>. This hammers AddItem from
    /// several background threads while the UI loop renders, and asserts every add lands exactly once — proving
    /// the adds were serialized onto the UI thread rather than racing on the (non-concurrent) dictionary.
    /// </summary>
    [Fact]
    public void ConcurrentAdds_AreSerializedOntoUiThread()
    {
        const int threads = 4;
        const int perThread = 300;

        var originalOut = Console.Out;
        Console.SetOut(TextWriter.Null);   // swallow ConsoleManager's ANSI output

        var list = new ListBox { SelectedForegroundColor = Color.White };
        var grid = new Grid([10], [24], [[list]]);
        Task run;

        try
        {
            run = UI.Start(grid, width: 24, height: 10, fps: 100,
                console: new ConsoleBuffer { Size = new Size(24, 10) });

            var tasks = Enumerable.Range(0, threads).Select(t => Task.Run(() =>
            {
                for (var i = 0; i < perThread; i++) list.AddItem($"t{t}-{i}");
            })).ToArray();

            Assert.True(Task.WaitAll(tasks, 10000), "Mutator tasks did not finish in time.");

            // Barrier: read the count on the UI thread after all marshaled adds have drained.
            var count = -1;
            Assert.True(UI.InvokeAsync(() => count = list.Items.Count).Wait(5000), "Barrier did not complete.");
            Assert.Equal(threads * perThread, count);
        }
        finally
        {
            UI.Stop();
            Console.SetOut(originalOut);
        }

        Assert.True(run.Wait(2000), "Stop() should complete the run task.");
    }

    [Fact]
    public void ConcurrentTreeAdds_AreSerializedOntoUiThread()
    {
        const int threads = 4;
        const int perThread = 200;

        var originalOut = Console.Out;
        Console.SetOut(TextWriter.Null);

        var tree = new Tree("Root");
        var grid = new Grid([10], [24], [[tree]]);
        Task run;

        try
        {
            run = UI.Start(grid, width: 24, height: 10, fps: 100,
                console: new ConsoleBuffer { Size = new Size(24, 10) });

            var tasks = Enumerable.Range(0, threads).Select(t => Task.Run(() =>
            {
                for (var i = 0; i < perThread; i++) tree.AddNode($"t{t}-{i}");
            })).ToArray();

            Assert.True(Task.WaitAll(tasks, 10000), "Mutator tasks did not finish in time.");

            var count = -1;
            Assert.True(UI.InvokeAsync(() => count = tree.Root.Children.Count).Wait(5000), "Barrier did not complete.");
            Assert.Equal(threads * perThread, count);
        }
        finally
        {
            UI.Stop();
            Console.SetOut(originalOut);
        }

        Assert.True(run.Wait(2000), "Stop() should complete the run task.");
    }

    [Fact]
    public void ConcurrentBarChartAdds_AreSerializedOntoUiThread()
    {
        const int threads = 4;
        const int perThread = 100;

        var originalOut = Console.Out;
        Console.SetOut(TextWriter.Null);

        var chart = new BarChart(ChartOrientation.Horizontal);
        var grid = new Grid([10], [30], [[chart]]);
        Task run;

        try
        {
            run = UI.Start(grid, width: 30, height: 10, fps: 100,
                console: new ConsoleBuffer { Size = new Size(30, 10) });

            var tasks = Enumerable.Range(0, threads).Select(t => Task.Run(() =>
            {
                for (var i = 0; i < perThread; i++) chart.AddItem($"t{t}-{i}", i, Color.Green);
            })).ToArray();

            Assert.True(Task.WaitAll(tasks, 15000), "Mutator tasks did not finish in time.");

            var count = -1;
            Assert.True(UI.InvokeAsync(() => count = chart.Data.Count).Wait(10000), "Barrier did not complete.");
            Assert.Equal(threads * perThread, count);
        }
        finally
        {
            UI.Stop();
            Console.SetOut(originalOut);
        }

        Assert.True(run.Wait(2000), "Stop() should complete the run task.");
    }
}
