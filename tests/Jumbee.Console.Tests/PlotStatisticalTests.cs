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
}
