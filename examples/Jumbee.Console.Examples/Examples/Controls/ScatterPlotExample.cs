namespace Jumbee.Console.Examples;

using System;
using System.Linq;

/// <summary>A scatter plot — points drawn as markers with no connecting line. Two <c>AddScatter</c> series share
/// the same axes/grid; the marker brush (Braille vs Quadrant here) sets the sub-cell resolution.</summary>
public sealed class ScatterPlotExample : Plot, IExample
{
    public ScatterPlotExample()
    {
        var xs = Enumerable.Range(0, 80).Select(i => -4 + i * 0.1).ToArray();
        AddScatter(xs, xs.Select(x => Math.Sin(x)).ToArray());
        AddScatter(xs, xs.Select(x => Math.Cos(x) * 0.6).ToArray(), PlotBrush.Quadrant);
    }

    public bool FillsPane => true;

    public string Category => "Controls";
    public string Title => "Scatter Plot";
    public string Description =>
        "Data points drawn as markers, no connecting line — two scatter series sharing one coordinate system.";
}
