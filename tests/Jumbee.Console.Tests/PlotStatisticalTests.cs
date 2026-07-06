namespace Jumbee.Console.Tests;

using System;
using System.Collections.Generic;
using System.Linq;

using Jumbee.Console.Snapshot;

using Xunit;

using CColor = ConsoleGUI.Data.Color;

/// <summary>Render tests for the box-and-whisker and error-bar plot elements: the expected box-drawing glyphs
/// appear, and degenerate data (single value, empty group) renders without throwing.</summary>
public class PlotStatisticalTests
{
    private static string Render(Plot plot) => ConsoleSnapshot.ToText(plot, 60, 20);

    [Fact]
    public void Box_FromSummary_DrawsBoxWhiskersAndMedian()
    {
        var plot = new Plot();
        // Two boxes with clear spread so the box, whiskers and median are all distinct rows.
        plot.AddBox(
            xs: [1, 2], mins: [0, 5], q1s: [3, 9], medians: [5, 12], q3s: [7, 15], maxes: [10, 20],
            color: new CColor(120, 200, 120), medianColor: new CColor(240, 200, 90));
        var text = Render(plot);

        // Box corners + median tees + whisker caps.
        Assert.Contains('┌', text);
        Assert.Contains('┐', text);
        Assert.Contains('└', text);
        Assert.Contains('┘', text);
        Assert.Contains('├', text);   // median line left tee
        Assert.Contains('┤', text);   // median line right tee
        Assert.Contains('┴', text);   // a whisker cap
    }

    [Fact]
    public void Boxes_FromRawGroups_ComputesQuartilesAndDraws()
    {
        var rng = new Random(3);
        var groups = Enumerable.Range(0, 4)
            .Select(g => (IReadOnlyList<double>)Enumerable.Range(0, 40).Select(_ => g * 10 + rng.NextDouble() * 8).ToArray())
            .ToList();

        var plot = new Plot();
        plot.AddBoxes(groups);
        var text = Render(plot);

        Assert.Contains('┌', text);   // at least one box drawn from the computed quartiles
        Assert.Contains('│', text);
    }

    [Fact]
    public void ErrorBars_DrawWhiskerCapsAndMarker()
    {
        var xs = new double[] { 1, 2, 3, 4 };
        var ys = new double[] { 5, 8, 6, 9 };
        var err = new double[] { 1.5, 2, 1, 2.5 };

        var plot = new Plot();
        plot.AddErrorBars(xs, ys, err, new CColor(230, 190, 90));
        var text = Render(plot);

        Assert.Contains('┬', text);   // top cap centre
        Assert.Contains('┴', text);   // bottom cap centre
        Assert.Contains('┼', text);   // point marker
    }

    [Fact]
    public void ErrorBars_Asymmetric_SpanLowToHigh()
    {
        var plot = new Plot();
        plot.AddErrorBars(xs: [1, 2, 3], ys: [10, 10, 10], errLows: [1, 2, 3], errHighs: [3, 2, 1], color: new CColor(200, 120, 200));
        Assert.Contains('┼', Render(plot));   // renders without throwing, marker present
    }

    [Fact]
    public void DegenerateData_DoesNotThrow()
    {
        // Single-value group (quartiles collapse), an empty group (skipped), and zero-error bars.
        var plot = new Plot();
        plot.AddBoxes([new double[] { 42 }, System.Array.Empty<double>(), new double[] { 1, 2, 3 }]);
        plot.AddErrorBars(xs: [1, 2], ys: [3, 4], errors: [0, 0], color: new CColor(90, 150, 240));
        var ex = Record.Exception(() => Render(plot));
        Assert.Null(ex);
    }

    private static readonly IReadOnlyList<IReadOnlyList<double>> ThreeSeries =
    [
        new double[] { 6, 9, 5, 8 },
        new double[] { 4, 7, 8, 3 },
        new double[] { 3, 2, 6, 5 },
    ];

    [Fact]
    public void GroupedBars_DrawSideBySideSubBars()
    {
        var plot = new Plot();
        plot.AddGroupedBars([1, 2, 3, 4], ThreeSeries);
        var text = Render(plot);

        Assert.Contains('█', text);
        // Grouped: within one group the sub-bars have different heights, so a row through the group mixes bar and
        // gap — i.e. some line contains both a full block and an interior run of spaces between blocks.
        var mixedRow = text.Split('\n').Any(line =>
        {
            int first = line.IndexOf('█');
            int last = line.LastIndexOf('█');
            return first >= 0 && last > first && line.Substring(first, last - first).Contains("  ");
        });
        Assert.True(mixedRow, "expected side-by-side sub-bars separated by gaps");
    }

    [Fact]
    public void StackedBars_AreSolidFullWidth_UnlikeGrouped()
    {
        // A stacked bar is one solid full-slot-width block up its whole height; a grouped bar narrows as the shorter
        // sub-bars end. So the stacked chart has more rows with a wide contiguous run of blocks. (The y-axis
        // auto-scales in both, so absolute height can't be compared — this structural difference can.)
        int WideRunRows(bool stacked)
        {
            var plot = new Plot();
            if (stacked) plot.AddStackedBars([1, 2, 3, 4], ThreeSeries);
            else plot.AddGroupedBars([1, 2, 3, 4], ThreeSeries);
            return Render(plot).Split('\n').Count(line => LongestRun(line, '█') >= 12);
        }

        Assert.True(WideRunRows(stacked: true) > WideRunRows(stacked: false),
            "a stacked bar should stay full-width up its height while a grouped bar narrows");
    }

    private static int LongestRun(string line, char ch)
    {
        int best = 0, run = 0;
        foreach (var c in line)
        {
            run = c == ch ? run + 1 : 0;
            if (run > best) best = run;
        }
        return best;
    }

    [Fact]
    public void HorizontalBars_LongerBarReachesFurtherRight()
    {
        var plot = new Plot();
        // The second category has the largest value, so its bar must extend furthest right.
        plot.AddHBars(positions: [1, 2, 3], values: [4, 20, 8], color: new CColor(120, 200, 120));
        var lines = Render(plot).Split('\n');

        int maxRight = lines.Max(l => l.LastIndexOf('█'));
        int shortRight = lines.Where(l => l.Contains('█'))
            .Select(l => l.LastIndexOf('█'))
            .Min();
        Assert.True(maxRight > shortRight, "the largest-value bar should extend further right than the smallest");
    }

    private static IReadOnlyList<IReadOnlyList<double>> Gradient(int rows, int cols) =>
        Enumerable.Range(0, rows)
            .Select(r => (IReadOnlyList<double>)Enumerable.Range(0, cols).Select(c => (double)(c + (rows - 1 - r))).ToArray())
            .ToArray();

    [Fact]
    public void Heatmap_FillsAreaAndFollowsColormap()
    {
        var plot = new Plot();
        plot.AddHeatmap(Gradient(8, 12), PlotColormap.Viridis);
        plot.ConfigureGrid(g => g.IsVisible = false);
        var buffer = ConsoleSnapshot.Render(plot, 60, 20);

        // The grid tiles the plot area with solid blocks.
        int blocks = 0;
        for (int y = 0; y < buffer.Size.Height; y++)
            for (int x = 0; x < buffer.Size.Width; x++)
                if (buffer[x, y].Character.Content == '█') blocks++;
        Assert.True(blocks > 400, $"expected the heatmap to fill the plot area, saw {blocks} blocks");

        // The lowest cell (bottom-left) maps near the Viridis low end (dark purple ~68,1,84), the highest
        // (top-right) near the high end (yellow ~253,231,37): the top-right is much greener, the bottom-left bluer.
        static (int r, int g, int b) FgAt(ConsoleBuffer buf, int x, int y)
        {
            var fg = buf[x, y].Character.Foreground!.Value;
            return (fg.Red, fg.Green, fg.Blue);
        }
        var low = FgAt(buffer, 4, buffer.Size.Height - 5);   // bottom-left region
        var high = FgAt(buffer, buffer.Size.Width - 6, 3);   // top-right region
        Assert.True(high.g > low.g + 60, $"top-right should be greener than bottom-left ({high} vs {low})");
        Assert.True(low.b > high.b, $"bottom-left (purple) should be bluer than top-right (yellow) ({low} vs {high})");
    }

    [Fact]
    public void Heatmap_DifferentColormaps_ProduceDifferentColors()
    {
        (int r, int g, int b) MidColor(PlotColormap cm)
        {
            var plot = new Plot();
            plot.AddHeatmap(Gradient(8, 12), cm);
            plot.ConfigureGrid(g => g.IsVisible = false);
            var buf = ConsoleSnapshot.Render(plot, 60, 20);
            var fg = buf[30, 10].Character.Foreground!.Value;
            return (fg.Red, fg.Green, fg.Blue);
        }

        Assert.NotEqual(MidColor(PlotColormap.Viridis), MidColor(PlotColormap.Heat));
        Assert.NotEqual(MidColor(PlotColormap.Grayscale), MidColor(PlotColormap.Cool));
    }

    [Fact]
    public void ConfusionMatrix_DrawsCellCountsWithContrastText()
    {
        // Strong diagonal (bright cells) with dim off-diagonal.
        var m = new List<IReadOnlyList<double>>
        {
            new double[] { 90, 2, 1 },
            new double[] { 3, 85, 4 },
            new double[] { 0, 5, 80 },
        };
        var plot = new Plot();
        plot.AddConfusionMatrix(m);
        plot.ConfigureGrid(g => g.IsVisible = false);
        var buffer = ConsoleSnapshot.Render(plot, 60, 20);

        // Collect every digit cell that carries a background (a real heatmap cell, not an axis label).
        var cellDigits = new List<(char ch, CColor fg, CColor bg)>();
        for (int y = 0; y < buffer.Size.Height; y++)
            for (int x = 0; x < buffer.Size.Width; x++)
            {
                var c = buffer[x, y].Character;
                if (c.Content is >= '0' and <= '9' && c.Background is { } bg && c.Foreground is { } fg)
                    cellDigits.Add((c.Content.Value, fg, bg));
            }

        Assert.NotEmpty(cellDigits);   // counts were drawn into the cells

        // Every labelled cell picks a readable contrast: dark text on a light cell, light text on a dark cell.
        static double Lum(CColor c) => 0.299 * c.Red + 0.587 * c.Green + 0.114 * c.Blue;
        Assert.All(cellDigits, d =>
            Assert.True((Lum(d.bg) > 140) == (Lum(d.fg) < 140),
                $"digit '{d.ch}' fg-luma {Lum(d.fg):F0} should contrast bg-luma {Lum(d.bg):F0}"));
    }

    [Fact]
    public void Heatmap_AllNaN_LeftBlank()
    {
        var nanGrid = Enumerable.Range(0, 4)
            .Select(_ => (IReadOnlyList<double>)Enumerable.Repeat(double.NaN, 4).ToArray())
            .ToArray();
        var plot = new Plot();
        plot.AddHeatmap(nanGrid);
        var buffer = ConsoleSnapshot.Render(plot, 40, 12);

        bool anyBlock = false;
        for (int y = 0; y < buffer.Size.Height && !anyBlock; y++)
            for (int x = 0; x < buffer.Size.Width; x++)
                if (buffer[x, y].Character.Content == '█') { anyBlock = true; break; }
        Assert.False(anyBlock, "an all-NaN heatmap should draw no cells");
    }
}
