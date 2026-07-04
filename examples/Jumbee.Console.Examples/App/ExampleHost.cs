namespace Jumbee.Console.Examples;

/// <summary>
/// The middle pane: a composite whose content is swapped to the selected example's live control. Swapping is just
/// re-calling <see cref="CompositeControl.SetContent"/> (it disposes the old content context and rebuilds), wrapping
/// the example control in a single-cell grid so it fills the pane.
/// </summary>
public sealed class ExampleHost : CompositeControl
{
    /// <summary>Shows <paramref name="content"/> (a control or layout) as the pane's live example, replacing
    /// whatever was there.</summary>
    public void Show(IFocusable content) => SetContent(new Grid([1], [1], [[content]]));
}
