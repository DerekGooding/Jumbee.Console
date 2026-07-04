namespace Jumbee.Console.Examples;

/// <summary>Primary/secondary buttons with hover + press states, wired to update a label.</summary>
public sealed class ButtonExample : ExampleBase
{
    public override string Category => "Controls";
    public override string Title => "Buttons";
    public override string Description => "Primary and secondary buttons with hover and press feedback.";

    public override IFocusable Build()
    {
        var result = new TextLabel(TextLabelOrientation.Horizontal, "Click a button…");
        var save = Button.Primary("Save");
        var cancel = Button.Secondary("Cancel");
        save.Clicked += (_, _) => result.Text = "Saved ✓";
        cancel.Clicked += (_, _) => result.Text = "Cancelled";
        return new VerticalStackPanel(save, cancel, result);
    }
}
