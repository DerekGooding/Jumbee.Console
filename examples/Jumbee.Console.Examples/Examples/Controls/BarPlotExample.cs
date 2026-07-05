namespace Jumbee.Console.Examples;

using System;
using System.Linq;

/// <summary>A bar plot — each point drawn as a filled bar from the baseline, with an eighth-block sub-cell top for
/// smooth heights. A <c>BarSeries</c> in the same polymorphic plot-element model as the line/scatter/stem plots, so
/// it shares the axes/grid and can be overlaid with them.</summary>
public sealed class BarPlotExample : Plot, IExample
{
    public BarPlotExample()
    {
        var xs = Enumerable.Range(1, 12).Select(i => (double)i).ToArray();
        var ys = xs.Select(x => 6 + 4 * Math.Sin(x * 0.6) + x * 0.2).ToArray();
        AddBars(xs, ys);
        ConfigureGrid(g => g.IsVisible = false);
    }

    public bool FillsPane => true;

    public string Category => "Controls";
    public string Title => "Bar Plot";
    public string Description =>
        "Filled bars from a baseline with eighth-block sub-cell tops, sharing the plot's axes and grid.";
}
