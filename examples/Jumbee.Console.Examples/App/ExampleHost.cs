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
        // Live examples (IActivatable) start/stop their feed as they're shown/replaced, so it only runs while visible.
        if (!ReferenceEquals(_active, content))
        {
            (_active as IActivatable)?.OnDeactivated();
            (content as IActivatable)?.OnActivated();
            _active = content;
        }

        // A fill-to-viewport example (e.g. Plot) must not be scrolled: fill the outer pane frame's viewport so the
        // host is given a bounded height instead of the unbounded scroll height (which balloons it to the size
        // clamp). Set before SetContent so the redraw it triggers re-reads FillsFrameViewport on the pane frame.
        _fills = content is IExample { FillsPane: true };
        var header = new TextLabel(TextLabelOrientation.Horizontal, description);
        SetContent(new DockPanel(DockedControlPlacement.Top, header, Framed(content)));
        // Swapping content doesn't change the host's own size (it's ballooned to the scroll clamp either way), so no
        // relayout bubbles to the pane frame — force it to re-read FillsFrameViewport and re-bound the host.
        Frame?.Relayout();
    }

    /// <summary>Stops the active example's background work (its <see cref="IActivatable"/> feed) — called on app quit
    /// so a live example's timers/threads don't keep running through shutdown. Idempotent.</summary>
    public void DeactivateActive() => (_active as IActivatable)?.OnDeactivated();

    // Reflects the current example: fill the pane frame's viewport for a fill-to-viewport example, otherwise keep the
    // default scrolling behaviour so tall examples (lists, editors) scroll.
    protected override bool FillsFrameViewport => _fills;

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
    private bool _fills;
}
