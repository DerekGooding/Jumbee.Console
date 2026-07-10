namespace Jumbee.Console.Examples;

using System;
using System.Linq;

/// <summary>
/// A line chart that <em>is</em> a <see cref="Plot"/>: add data with <c>AddSeries</c>, tune it with <c>Configure*</c>.
/// Fills its pane and re-fits when the pane is resized.
/// </summary>
public sealed class PlotExample : Plot, IExample
{
    public PlotExample()
    {
        // Two smooth Braille curves over [-2π, 2π]: a sine and a decaying cosine.
        var xs = Enumerable.Range(0, 201).Select(i => -2 * Math.PI + i * (4 * Math.PI / 200)).ToArray();
        AddSeries(xs, xs.Select(Math.Sin).ToArray());
        AddSeries(xs, xs.Select(x => Math.Cos(x) * Math.Exp(-Math.Abs(x) * 0.2)).ToArray());
        ConfigureTicks(t => t.Labels.Format = "0.#");
    }

    #region IExample
    string IExample.Category => "Controls";
    string IExample.Title => "Plot";
    string IExample.Description =>
        "A line/scatter chart backed by ConsolePlot with smooth Braille curves, axes, grid and ticks.";
    #endregion
}
