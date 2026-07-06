namespace Jumbee.Console.Examples;

using System;
using System.Collections.Generic;
using System.Linq;

using CColor = ConsoleGUI.Data.Color;

/// <summary>A box-and-whisker plot — one box per group showing the quartiles (Q1–Q3 box, median line) with whiskers
/// to the min/max, drawn with box-drawing glyphs. A <c>BoxSeries</c> in the same polymorphic plot-element model as
/// the other plot types; the quartiles are computed from raw sample groups by <c>AddBoxes</c>.</summary>
public sealed class BoxPlotExample : Plot, IExample
{
    public BoxPlotExample()
    {
        // Six deterministic (seeded) sample groups with drifting centre and spread, so each box differs.
        var rng = new Random(11);
        var groups = new List<IReadOnlyList<double>>();
        for (int g = 0; g < 6; g++)
        {
            double centre = 20 + g * 6;
            double spread = 6 + g * 1.5;
            groups.Add(Enumerable.Range(0, 60)
                .Select(_ => centre + NextGaussian(rng) * spread)
                .ToArray());
        }

        AddBoxes(groups, medianColor: new CColor(240, 200, 90));
        ConfigureGrid(g => g.IsVisible = true);
    }

    // Box-Muller: a standard-normal sample from the uniform RNG.
    private static double NextGaussian(Random rng)
    {
        double u1 = 1.0 - rng.NextDouble();
        double u2 = 1.0 - rng.NextDouble();
        return Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Cos(2.0 * Math.PI * u2);
    }

    public bool FillsPane => true;

    public string Category => "Controls";
    public string Title => "Box Plot";
    public string Description =>
        "Box-and-whisker plot — Q1–Q3 box with median line and min/max whiskers, quartiles computed from raw samples.";
}
