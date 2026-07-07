namespace Jumbee.Console.Tests;

using System;
using System.Linq;

using Jumbee.Console.Snapshot;

using Xunit;

using CColor = ConsoleGUI.Data.Color;

/// <summary>Live-update series handles: a held <see cref="PlotSeries"/> updates the plot in place (no Clear/re-add).</summary>
public class PlotLiveTests
{
    private static string Render(Plot p) => ConsoleSnapshot.ToText(p, 50, 16);
    private static bool HasBraille(string s) => s.IndexOfAny([.. Braille()]) >= 0;
    private static System.Collections.Generic.IEnumerable<char> Braille()
    {
        for (char c = '⠀'; c <= '⣿'; c++) yield return c;
    }

    [Fact]
    public void LiveBars_SetValues_UpdatesInPlace()
    {
        var plot = new Plot();
        var bars = plot.AddLiveBars(new CColor(90, 160, 240));
        plot.ConfigureGrid(g => g.IsVisible = false);

        bars.SetValues([2, 8, 4]);
        var first = Render(plot);
        Assert.Contains('█', first);

        bars.SetValues([8, 2, 4]);   // same handle, new data, no Clear/re-add
        var second = Render(plot);
        Assert.Contains('█', second);
        Assert.NotEqual(first, second);   // the plot reflects the new data
    }

    [Fact]
    public void LiveSeries_PushRollingWindow_RendersAndDoesNotThrow()
    {
        var plot = new Plot();
        var line = plot.AddLiveSeries(new CColor(240, 120, 100));
        plot.ConfigureGrid(g => g.IsVisible = false);

        // Stream 200 points through a 40-point window — the rolling window keeps memory/render bounded.
        var ex = Record.Exception(() =>
        {
            for (int i = 0; i < 200; i++) line.Push(i, Math.Sin(i * 0.2) * 10, maxPoints: 40);
        });
        Assert.Null(ex);
        Assert.True(HasBraille(Render(plot)), "a live line should render braille points");
    }

    [Fact]
    public void FixedYRange_PinsAxis_RegardlessOfData()
    {
        static string ToText(Plot p) => ConsoleSnapshot.ToText(p, 44, 16);

        // Small data (~10..25): auto-scaled, the axis tops out near 25 → no "80" tick.
        var auto = new Plot();
        auto.ConfigureGrid(g => g.IsVisible = false);
        auto.AddLiveSeries().SetData([0, 1, 2, 3], [10, 20, 15, 25]);
        Assert.DoesNotContain("80", ToText(auto));

        // Same data, axis pinned to 0..100 → a high tick (80) is present regardless of the small data...
        var pinned = new Plot();
        pinned.ConfigureGrid(g => g.IsVisible = false);
        pinned.SetYRange(0, 100);
        var s = pinned.AddLiveSeries();
        s.SetData([0, 1, 2, 3], [10, 20, 15, 25]);
        Assert.Contains("80", ToText(pinned));

        // ...and the axis stays put when the data changes completely (it doesn't rescale/jump).
        s.SetData([0, 1, 2, 3], [45, 55, 50, 60]);
        Assert.Contains("80", ToText(pinned));
    }

    [Fact]
    public void XWindow_ShowsLatestWindow_AndStaysNonNegative()
    {
        var p = new Plot();
        p.ConfigureGrid(g => g.IsVisible = false);
        p.SetXWindow(20);
        var s = p.AddLiveSeries();

        // Latest data around x = 80..100: the sliding window shows [80, 100], so a high x tick is present (the axis
        // followed the data forward) — the old low-x points scrolled off.
        s.SetData(Enumerable.Range(80, 21).Select(i => (double)i).ToArray(),
                  Enumerable.Range(0, 21).Select(i => (double)(i % 6)).ToArray());
        Assert.Contains("100", ConsoleSnapshot.ToText(p, 50, 12));

        // Early on (data hasn't filled the window), the window is floored at 0 — never a negative x axis.
        s.SetData([0, 1, 2, 3, 4, 5], [1, 3, 2, 4, 3, 5]);
        var early = ConsoleSnapshot.ToText(p, 50, 12);
        Assert.DoesNotContain("-", early.Split('\n').Last());   // the x-axis label row has no negative values
    }

    [Fact]
    public void Scroll_KeepsFixedStripAxis_NoMatterHowManyValues()
    {
        var p = new Plot();
        p.ConfigureGrid(g => g.IsVisible = false);
        p.SetXRange(0, 19);
        var s = p.AddLiveSeries();

        // Scroll 1000 values through a 20-wide strip: x stays 0..19, so the axis never grows past ~19 — no large,
        // ever-increasing x labels (which is what a Push-based ever-increasing counter would produce).
        for (int i = 0; i < 1000; i++) s.Scroll(50 + 20 * Math.Sin(i * 0.2), 20);
        var text = ConsoleSnapshot.ToText(p, 44, 12);

        Assert.DoesNotContain("100", text);   // would appear as an x tick only if the axis had grown
        Assert.DoesNotContain("500", text);
    }

    [Fact]
    public void LiveSeries_SetData_ThenClear()
    {
        var plot = new Plot();
        var line = plot.AddLiveSeries();
        line.SetData([0, 1, 2, 3], [0, 5, 2, 8]);
        Assert.True(HasBraille(Render(plot)));

        line.Clear();
        Assert.False(HasBraille(Render(plot)), "a cleared live series draws no points");
    }
}
