namespace Jumbee.Console.Examples;

using S = Spectre.Console;

/// <summary>Resizable panes: drag the divider, or focus it and press the arrows (Shift = bigger steps). Nest them
/// for richer layouts — this is how the browser's own shell is built.</summary>
public sealed class SplitPanelExample : ExampleBase
{
    public override string Category => "Flexibility";
    public override string Title => "Resizable Layout";
    public override string Description =>
        "A SplitPanel gives two panes a draggable divider. Drag it, or focus the divider and use the arrow keys.";

    public override IFocusable Build()
    {
        var left = new ListBox("Drag the divider →", "…or focus it", "and press ← →", "Shift = bigger steps")
            .WithFrame(title: "Left");
        var right = new SpectreControl<S.Panel>(new S.Panel(
            new S.Markup("[bold]Right pane[/]\n\nThe divider between us can be dragged with the mouse,\n" +
                         "or nudged with the arrow keys when it has focus."))
        {
            Border = S.BoxBorder.Rounded,
            Padding = new S.Padding(1, 1, 1, 1),
            Expand = true,
        });
        return new SplitPanel(SplitOrientation.Horizontal, left, right, splitPosition: 26);
    }
}
