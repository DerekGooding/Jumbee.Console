namespace Jumbee.Console.Examples;

using System;
using System.Linq;

/// <summary>
/// A bar plot — each point drawn as a filled bar from the baseline, with eighth-block sub-cell tops.
/// A <c>BarSeries</c> that shares the plot's axes and grid.
/// </summary>
public sealed class BarPlotExample : Plot, IExample
{
    public BarPlotExample()
    {
        var xs = Enumerable.Range(1, 12).Select(i => (double)i).ToArray();
        var ys = xs.Select(x => 6 + 4 * Math.Sin(x * 0.6) + x * 0.2).ToArray();
        AddBars(xs, ys);
        ConfigureGrid(g => g.IsVisible = false);
    }

    #region IExample
    string IExample.Category => "Visualization";
    string IExample.Title => "Bar Plot";
    string IExample.Description =>
        "Filled bars from a baseline with eighth-block sub-cell tops, sharing the plot's axes and grid.";
    #endregion
}
