namespace Jumbee.Console.Examples;

/// <summary>
/// The middle pane: a composite whose content is swapped to the selected example's live control. Swapping is just
/// re-calling <see cref="CompositeControl.SetContent"/> (it disposes the old content context and rebuilds). The demo
/// is placed in a <see cref="DockPanel"/>'s fill slot (the fill slot expands to the pane — a fixed-size Grid cell
/// would clip it), with a one-line description docked above it.
/// </summary>
public sealed class ExampleHost : CompositeControl
{
    /// <summary>Shows <paramref name="content"/> (a control or layout) as the pane's live example under a one-line
    /// <paramref name="description"/>, replacing whatever was there.</summary>
    public void Show(IFocusable content, string description)
    {
        var header = new TextLabel(TextLabelOrientation.Horizontal, description);
        SetContent(new DockPanel(DockedControlPlacement.Top, header, content));
    }
}
