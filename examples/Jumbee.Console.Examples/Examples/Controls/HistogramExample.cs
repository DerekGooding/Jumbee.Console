namespace Jumbee.Console.Examples;

using System;
using System.Linq;

/// <summary>A histogram — <c>AddHistogram</c> bins a set of values and draws each bin as a touching bar (built on
/// the same <c>DrawBars</c> primitive as the bar plot, so binning is pure data-prep with no new drawing code).</summary>
public sealed class HistogramExample : Plot, IExample
{
    public HistogramExample()
    {
        // A deterministic ~normal sample (Box-Muller with a seeded RNG) so the bell shape is stable.
        var rng = new Random(42);
        var data = Enumerable.Range(0, 600).Select(_ =>
        {
            double u1 = 1.0 - rng.NextDouble(), u2 = 1.0 - rng.NextDouble();
            return 50 + 12 * Math.Sqrt(-2 * Math.Log(u1)) * Math.Cos(2 * Math.PI * u2);
        }).ToArray();
        AddHistogram(data);
        ConfigureGrid(g => g.IsVisible = false);
    }

    public bool FillsPane => true;

    public string Category => "Controls";
    public string Title => "Histogram";
    public string Description =>
        "Values binned into buckets and drawn as touching bars — the distribution's shape at a glance.";
}
