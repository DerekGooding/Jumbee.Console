namespace Jumbee.Console.Examples;

using System;
using System.Linq;

/// <summary>
/// A histogram — <c>AddHistogram</c> bins a set of values and draws each bin as a touching bar.
/// </summary>
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

    #region IExample
    bool IExample.FillsPane => true;
    string IExample.Category => "Controls";
    string IExample.Title => "Histogram";
    string IExample.Description =>
        "Values binned into buckets and drawn as touching bars — the distribution's shape at a glance.";
    #endregion
}
