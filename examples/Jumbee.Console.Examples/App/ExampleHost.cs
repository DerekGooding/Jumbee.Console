namespace Jumbee.Console.Examples;

using System.Collections.Generic;

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
        // Live examples (IActivatableExample) start/stop their feed as they're shown/replaced, so it only runs while visible.
        if (!ReferenceEquals(_active, content))
        {
            (_active as IActivatableExample)?.OnDeactivated();
            (content as IActivatableExample)?.OnActivated();
            _active = content;
        }

        var header = new TextLabel(TextLabelOrientation.Horizontal, description);
        SetContent(new DockPanel(DockedControlPlacement.Top, header, Framed(content)));
    }

    /// <summary>Stops the active example's background work (its <see cref="IActivatableExample"/> feed) — called on app quit
    /// so a live example's timers/threads don't keep running through shutdown. Idempotent.</summary>
    public void DeactivateActive() => (_active as IActivatableExample)?.OnDeactivated();

    // Always fill the pane frame's viewport: the host never scrolls itself. A fill-to-viewport example (Plot, Canvas…)
    // then fills the bounded height; a scrollable one scrolls inside its own inner frame (see Framed) with a single
    // scrollbar. Ballooning the host instead would make the pane frame a second, redundant scroller.
    protected override bool FillsFrameViewport => true;

    // A scrollable Control (ListBox, editors…) needs a ControlFrame to scroll — the example is a bare control, so we
    // give it a borderless frame here (the pane's own frame supplies the visible border/title). Layouts arrange their
    // own children and pass through. Framed once per example instance and cached, so re-selecting keeps scroll state.
    private IFocusable Framed(IFocusable content)
    {
        if (content is not Control control) return content;
        if (!_framed.TryGetValue(control, out var framed))
            _framed[control] = framed = control.WithFrame(borderStyle: BorderStyle.None);
        return framed;
    }

    private readonly Dictionary<Control, IFocusable> _framed = new();
    private IFocusable? _active;
}
