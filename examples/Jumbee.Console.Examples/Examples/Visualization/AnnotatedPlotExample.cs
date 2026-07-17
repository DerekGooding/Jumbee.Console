namespace Jumbee.Console.Examples;

using System;
using System.Linq;

using CColor = ConsoleGUI.Data.Color;

/// <summary>
/// Point annotations — <c>AddLabel</c> attaches coloured text to a data coordinate.
/// Labels don't rescale the axes; alignment and cell offsets control placement.
/// </summary>
public sealed class AnnotatedPlotExample : Plot, IExample
{
    public AnnotatedPlotExample()
    {
        var xs = Enumerable.Range(0, 60).Select(i => i * 0.2).ToArray();
        var ys = xs.Select(x => Math.Sin(x) * Math.Exp(-x * 0.15)).ToArray();
        AddSeries(xs, ys);

        // Tag the peak and the trough with coloured labels sitting just off the point.
        int iMax = Array.IndexOf(ys, ys.Max());
        int iMin = Array.IndexOf(ys, ys.Min());
        AddLabel(xs[iMax], ys[iMax], " peak ", fg: new CColor(20, 24, 20), bg: new CColor(90, 200, 120), dy: 1);
        AddLabel(xs[iMin], ys[iMin], " trough ", fg: CColor.White, bg: new CColor(210, 80, 80), dy: -1);
        AddLabel(xs[^1], ys[^1], "→ settles", fg: new CColor(240, 200, 90), align: PlotLabelAlign.Left, dx: 1, dy: 0);
    }

    #region IExample
    string IExample.Category => "Visualization";
    string IExample.Title => "Annotations";
    string IExample.Description =>
        "Label points with foreground/background colours — anchored to data coordinates, without rescaling the axes.";
    #endregion
}
