namespace Jumbee.Console.Examples;

using System.Collections.Generic;

/// <summary>A confusion matrix — an annotated heatmap where each cell shows its count in readable-contrast text on
/// the cell's own colour (the first use of the per-cell background work, change B). Row = actual class (top to
/// bottom), column = predicted class; the bright diagonal is the correctly-classified count.</summary>
public sealed class ConfusionMatrixExample : Plot, IExample
{
    public ConfusionMatrixExample()
    {
        // A 5-class classifier's counts: a strong diagonal with some believable off-diagonal confusion.
        var counts = new List<IReadOnlyList<double>>
        {
            new double[] { 58, 3, 1, 0, 2 },
            new double[] { 4, 47, 6, 1, 0 },
            new double[] { 1, 5, 51, 8, 2 },
            new double[] { 0, 2, 7, 44, 3 },
            new double[] { 2, 1, 0, 4, 63 },
        };

        AddConfusionMatrix(counts);
        ConfigureGrid(g => g.IsVisible = false);
    }

    public bool FillsPane => true;

    public string Category => "Controls";
    public string Title => "Confusion Matrix";
    public string Description =>
        "An annotated heatmap — counts drawn in each cell with contrast text on the cell colour (per-cell background).";
}
