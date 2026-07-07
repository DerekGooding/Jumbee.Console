namespace Jumbee.Console.Tests;

using System;

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
