namespace Jumbee.Console.Examples;

/// <summary>Modal dialogs over the ambient overlay — a confirm (Yes/No) and a message (OK), both reporting back.
/// The kind of GUI affordance that's awkward in an immediate-mode TUI but natural here.</summary>
public sealed class DialogExample : ExampleBase
{
    public override string Category => "Flexibility";
    public override string Title => "Modal Dialogs";
    public override string Description =>
        "Real modal dialogs: they take exclusive focus, dim the layer behind, and report a result. Confirm / message / custom content.";

    public override IFocusable Build()
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
        return new VerticalStackPanel(confirm, message, result);
    }
}
