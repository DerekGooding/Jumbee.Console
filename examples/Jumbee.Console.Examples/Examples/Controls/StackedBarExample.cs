namespace Jumbee.Console.Examples;

using System.Collections.Generic;

/// <summary>A stacked bar chart — three components stacked from the baseline at each x, so each bar's total height
/// is the sum. A <c>MultiBarSeries</c> (stacked mode); segments are full cells between rounded cumulative
/// boundaries so they abut exactly.</summary>
public sealed class StackedBarExample : Plot, IExample
{
    public StackedBarExample()
    {
        double[] xs = [1, 2, 3, 4, 5];
        IReadOnlyList<IReadOnlyList<double>> series =
        [
            [8, 10, 7, 12, 9],    // base layer
            [5, 6, 9, 4, 7],      // middle layer
            [3, 4, 2, 6, 5],      // top layer
        ];
        Color[] colors = [new(89, 145, 240), new(120, 200, 120), new(240, 200, 90)];

        AddStackedBars(xs, series, colors);
        ConfigureGrid(g => g.IsVisible = false);
    }

    public bool FillsPane => true;

    public string Category => "Controls";
    public string Title => "Stacked Bars";
    public string Description =>
        "Series stacked from the baseline at each x — each bar's total height is the sum of its components.";
}
