namespace Jumbee.Console.Examples;

using CColor = ConsoleGUI.Data.Color;

/// <summary>
/// A horizontal bar chart — each category sits at a Y position and its bar grows along X from the baseline.
/// An <c>HBarSeries</c> with a sub-cell eighth-block right edge.
/// </summary>
public sealed class HorizontalBarExample : Plot, IExample
{
    public HorizontalBarExample()
    {
        // Categories on the Y axis (1..6, bottom to top), each with a value extending rightward.
        double[] positions = [1, 2, 3, 4, 5, 6];
        double[] values = [42, 31, 58, 24, 67, 15];

        AddHBars(positions, values, new CColor(120, 200, 160));
        ConfigureGrid(g => g.IsVisible = false);
    }

    #region IExample
    string IExample.Category => "Visualization";
    string IExample.Title => "Horizontal Bars";
    string IExample.Description =>
        "Bars growing along the X axis from a baseline, one per category on the Y axis, with sub-cell right edges.";
    #endregion
}
