namespace Jumbee.Console.Examples;

using System.Collections.Generic;
using System.Linq;

using CColor = ConsoleGUI.Data.Color;

/// <summary>A grouped bar chart — three product series drawn side by side within each quarter's slot, each in its
/// own colour. A <c>MultiBarSeries</c> (grouped mode) in the same polymorphic plot-element model as the other plot
/// types, reusing the bar slot machinery so the sub-bars tile the group exactly on resize.</summary>
public sealed class GroupedBarExample : Plot, IExample
{
    public GroupedBarExample()
    {
        var quarters = new double[] { 1, 2, 3, 4 };
        var series = new List<IReadOnlyList<double>>
        {
            new double[] { 18, 24, 21, 30 },   // Product A
            new double[] { 12, 16, 22, 19 },   // Product B
            new double[] { 9, 14, 11, 17 },    // Product C
        };
        var colors = new[] { new CColor(89, 145, 240), new CColor(240, 120, 100), new CColor(120, 200, 120) };

        AddGroupedBars(quarters, series, colors);
        ConfigureGrid(g => g.IsVisible = false);
    }

    public bool FillsPane => true;

    public string Category => "Controls";
    public string Title => "Grouped Bars";
    public string Description =>
        "Multiple series drawn side by side within each x slot — the classic grouped bar chart, sharing the axes.";
}
