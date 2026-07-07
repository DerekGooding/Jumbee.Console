namespace Jumbee.Console.Examples;

using System.Collections.Generic;

/// <summary>A confusion matrix — an annotated heatmap where each cell shows its count in readable-contrast text on
/// the cell's own colour (the per-cell background work, change B), with the class names as categorical axis ticks
/// at the cell centres. Row = actual class (top to bottom), column = predicted class; the bright diagonal is the
/// correctly-classified count.</summary>
public sealed class ConfusionMatrixExample : Plot, IExample
{
    public ConfusionMatrixExample()
    {
        string[] classes = ["cat", "dog", "bird", "fish", "frog"];
        // A 5-class classifier's counts: a strong diagonal with some believable off-diagonal confusion.
        IReadOnlyList<IReadOnlyList<double>> counts =
        [
            [58, 3, 1, 0, 2],
            [4, 47, 6, 1, 0],
            [1, 5, 51, 8, 2],
            [0, 2, 7, 44, 3],
            [2, 1, 0, 4, 63],
        ];

        AddConfusionMatrix(counts, rowLabels: classes, colLabels: classes);
        ConfigureGrid(g => g.IsVisible = false);
    }

    public bool FillsPane => true;

    public string Category => "Controls";
    public string Title => "Confusion Matrix";
    public string Description =>
        "An annotated heatmap — counts drawn in each cell with contrast text on the cell colour (per-cell background).";
}
