namespace Jumbee.Console.Examples;

using System;
using System.Linq;

/// <summary>A line chart — it simply <em>is</em> a <see cref="Plot"/>, backed by the ConsolePlot library. Add data
/// with <c>AddSeries</c>; tune the axes/grid/ticks with the <c>Configure*</c> methods. The plot fills its pane and
/// re-draws to fit whenever the pane is resized (drag the split divider to watch it re-fit).</summary>
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

    public bool FillsPane => true;   // re-fits to the pane; must not be scrolled by the host

    public string Category => "Controls";
    public string Title => "Plot";
    public string Description =>
        "A line/scatter chart backed by ConsolePlot, rendered into the buffer with smooth Braille curves, axes, grid and ticks.";
}
