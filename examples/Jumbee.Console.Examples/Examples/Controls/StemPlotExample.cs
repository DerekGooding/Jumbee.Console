namespace Jumbee.Console.Examples;

using System;
using System.Linq;

/// <summary>
/// A stem plot — a vertical line rises from the baseline (0) to each point, capped with a marker.
/// Good for discrete / impulse signals.
/// </summary>
public sealed class StemPlotExample : Plot, IExample
{
    public StemPlotExample()
    {
        var xs = Enumerable.Range(0, 28).Select(i => (double)i).ToArray();
        var ys = xs.Select(x => Math.Sin(x * 0.5) * Math.Exp(-x * 0.06)).ToArray();
        AddStem(xs, ys);
    }

    #region IExample
    string IExample.Category => "Controls";
    string IExample.Title => "Stem Plot";
    string IExample.Description =>
        "A vertical stem from the baseline to each point, capped with a marker — for discrete / impulse signals.";
    #endregion
}
