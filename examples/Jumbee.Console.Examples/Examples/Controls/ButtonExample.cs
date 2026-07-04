namespace Jumbee.Console.Examples;

/// <summary>Primary/secondary buttons with hover + press states, wired to update a label. A small composite (a
/// vertical stack of controls) that also implements <see cref="IExample"/>.</summary>
public sealed class ButtonExample : CompositeControl, IExample
{
    public ButtonExample()
    {
        var result = new TextLabel(TextLabelOrientation.Horizontal, "Click a button…");
        var save = Button.Primary("Save");
        var cancel = Button.Secondary("Cancel");
        save.Clicked += (_, _) => result.Text = "Saved ✓";
        cancel.Clicked += (_, _) => result.Text = "Cancelled";
        SetContent(new VerticalStackPanel(save, cancel, result));
    }

    public string Category => "Controls";
    public string Title => "Buttons";
    public string Description => "Primary and secondary buttons with hover and press feedback.";
}
