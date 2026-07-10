namespace Jumbee.Console.Examples;

/// <summary>
/// Primary/secondary buttons with hover + press states, wired to update a label.
/// </summary>
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

    #region IExample
    string IExample.Category => "Controls";
    string IExample.Title => "Buttons";
    string IExample.Description => "Primary and secondary buttons with hover and press feedback.";
    #endregion
}
