namespace Jumbee.Console.Examples;

using System;
using System.Linq;

using CColor = ConsoleGUI.Data.Color;

/// <summary>
/// An error-bar plot — a smooth line with vertical ±error whiskers (caps and a centre marker) at each point.
/// An <c>ErrorBarSeries</c> overlaid on a line series, sharing the plot's axes.
/// </summary>
public sealed class ErrorBarExample : Plot, IExample
{
    public ErrorBarExample()
    {
        var xs = Enumerable.Range(0, 11).Select(i => (double)i).ToArray();
        var ys = xs.Select(x => 10 + 5 * Math.Sin(x * 0.5)).ToArray();
        // Error grows with x, so the bars visibly widen across the series.
        var errors = xs.Select(x => 0.8 + x * 0.25).ToArray();

        AddSeries(xs, ys, PlotBrush.Braille, new CColor(120, 200, 120));
        AddErrorBars(xs, ys, errors, new CColor(230, 190, 90));
        ConfigureGrid(g => g.IsVisible = true);
    }

    #region IExample
    bool IExample.FillsPane => true;
    string IExample.Category => "Controls";
    string IExample.Title => "Error Bars";
    string IExample.Description =>
        "A line series with vertical error bars — ±error whiskers with caps and a centre marker — sharing the axes.";
    #endregion
}
