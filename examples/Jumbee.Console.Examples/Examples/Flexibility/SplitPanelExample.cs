namespace Jumbee.Console.Examples;

using S = Spectre.Console;

/// <summary>
/// Resizable panes — it <em>is</em> a <see cref="SplitPanel"/>. Drag the divider, or focus it and press the arrows.
/// </summary>
public sealed class SplitPanelExample : SplitPanel, IExample
{
    public SplitPanelExample() : base(SplitOrientation.Horizontal, BuildLeft(), BuildRight(), splitPosition: 26) { }

    private static IFocusable BuildLeft() =>
        new ListBox("Drag the divider →", "…or focus it", "and press ← →", "Shift = bigger steps")
            .WithFrame(title: "Left");

    private static IFocusable BuildRight() =>
        new SpectreControl<S.Panel>(new S.Panel(
            new S.Markup("[bold]Right pane[/]\n\nThe divider between us can be dragged with the mouse,\n" +
                         "or nudged with the arrow keys when it has focus."))
        {
            Border = S.BoxBorder.Rounded,
            Padding = new S.Padding(1, 1, 1, 1),
            Expand = true,
        });

    #region IExample
    string IExample.Category => "Flexibility";
    string IExample.Title => "Resizable Layout";
    string IExample.Description =>
        "A SplitPanel gives two panes a draggable divider. Drag it, or focus the divider and use the arrow keys.";
    #endregion
}
