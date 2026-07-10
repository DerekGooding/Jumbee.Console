namespace Jumbee.Console.Examples;

/// <summary>
/// Modal dialogs over the ambient overlay — a confirm (Yes/No) and a message (OK), both reporting back.
/// </summary>
public sealed class DialogExample : CompositeControl, IExample
{
    public DialogExample()
    {
        var result = new TextLabel(TextLabelOrientation.Horizontal, "Open a dialog…");
        var confirm = Button.Primary("Confirm…");
        var message = Button.Secondary("Message…");
        confirm.Clicked += (_, _) =>
            Dialog.Confirm("Delete file", "Delete report.pdf? This cannot be undone.",
                yes => result.Text = yes ? "Deleted ✓" : "Kept");
        message.Clicked += (_, _) =>
            Dialog.Message("About", "Jumbee.Console modal dialog — Esc or a button dismisses it.",
                () => result.Text = "Dismissed");
        SetContent(new VerticalStackPanel(confirm, message, result));
    }

    #region IExample
    string IExample.Category => "Flexibility";
    string IExample.Title => "Modal Dialogs";
    string IExample.Description =>
        "Real modal dialogs: they take exclusive focus, dim the layer behind, and report a result.";
    #endregion
}
