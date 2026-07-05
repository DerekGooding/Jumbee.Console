namespace Jumbee.Console;

using System;
using System.Collections.Generic;

using ConsoleGUI.Input;
using ConsoleGUI.Space;

using Spectre.Console.Rendering;

/// <summary>
/// The draggable divider between a <see cref="SplitPanel"/>'s two panes. Focusable: drag it with the mouse (it
/// captures so the drag stays live off-control) or, when focused, resize with the arrow keys (Shift = larger step).
/// Raises <see cref="Dragged"/> / <see cref="Nudged"/> with a signed cell delta along the split axis; the owning
/// <see cref="SplitPanel"/> applies them to <see cref="SplitPanel.SplitPosition"/>.
/// </summary>
public class SplitDivider : RenderableControl
{
    #region Constructors
    internal SplitDivider(SplitOrientation orientation)
    {
        _orientation = orientation;
        if (orientation == SplitOrientation.Horizontal) Width = 1; else Height = 1;   // cross-axis fills
        ApplyTheme();
    }
    #endregion

    #region Events
    /// <summary>Raised while dragging, with a signed cell delta along the split axis.</summary>
    internal event Action<int>? Dragged;

    /// <summary>Raised on a keyboard resize, with a signed cell step along the split axis.</summary>
    internal event Action<int>? Nudged;
    #endregion

    #region Properties
    public override bool HandlesInput => true;
    protected override bool RendersOwnFocus => true;   // recolours the divider when focused (grabbed for resize)
    #endregion

    #region Methods
    protected override void ApplyTheme()
    {
        if (!IsThemeOverridden(nameof(LineStyle))) _lineStyle = UI.StyleTheme.TextMuted;
        if (!IsThemeOverridden(nameof(HoverStyle))) _hoverStyle = UI.StyleTheme.Hover;
        if (!IsThemeOverridden(nameof(ActiveStyle))) _activeStyle = UI.StyleTheme.Selection;
    }

    /// <summary>Style of the divider line at rest. Defaults to <see cref="IStyleTheme.TextMuted"/>.</summary>
    public Style LineStyle { get => _lineStyle; set => SetAtomicProperty(ref _lineStyle, value, themeOverride: true); }

    /// <summary>Style when hovered. Defaults to <see cref="IStyleTheme.Hover"/>.</summary>
    public Style HoverStyle { get => _hoverStyle; set => SetAtomicProperty(ref _hoverStyle, value, themeOverride: true); }

    /// <summary>Style when focused or being dragged. Defaults to <see cref="IStyleTheme.Selection"/>.</summary>
    public Style ActiveStyle { get => _activeStyle; set => SetAtomicProperty(ref _activeStyle, value, themeOverride: true); }

    protected override IEnumerable<Segment> Render(RenderOptions options, int maxWidth)
    {
        // Bright ActiveStyle only while actually dragging; hover OR keyboard-focus shows the milder HoverStyle; else
        // the resting line. Focus deliberately doesn't use ActiveStyle, so grabbing the divider (a click focuses it)
        // then moving the mouse away doesn't leave it stuck brightly highlighted.
        var style = (_dragging ? _activeStyle : IsMouseOver || IsFocused ? _hoverStyle : _lineStyle).SpectreConsoleStyle;
        if (_orientation == SplitOrientation.Horizontal)
        {
            var rows = Math.Max(1, ActualHeight);
            for (var r = 0; r < rows; r++)
            {
                yield return new Segment("│", style);   // │
                if (r < rows - 1) yield return Segment.LineBreak;
            }
        }
        else
        {
            var width = Math.Max(1, ActualWidth > 0 ? ActualWidth : maxWidth);
            yield return new Segment(new string('─', width), style);   // ─
        }
    }

    protected override void OnMousePress(Position position)
    {
        _dragging = true;
        _dragLast = Axis(position);
        CaptureMouse();   // keep receiving move/up even as the pointer leaves this 1-cell divider
        Invalidate();
    }

    protected override void OnMouseMove(Position position)
    {
        if (!_dragging) return;
        var cur = Axis(position);
        var delta = cur - _dragLast;
        if (delta != 0) { _dragLast = cur; Dragged?.Invoke(delta); }
    }

    protected override void OnMouseRelease(Position position)
    {
        if (!_dragging) return;
        _dragging = false;
        ReleaseMouse();
        Invalidate();
    }

    protected override void OnInput(InputEvent inputEvent)
    {
        var big = (inputEvent.Key.Modifiers & ConsoleModifiers.Shift) != 0 ? 5 : 1;
        var step = _orientation == SplitOrientation.Horizontal
            ? (inputEvent.Key.Key == ConsoleKey.LeftArrow ? -big : inputEvent.Key.Key == ConsoleKey.RightArrow ? big : 0)
            : (inputEvent.Key.Key == ConsoleKey.UpArrow ? -big : inputEvent.Key.Key == ConsoleKey.DownArrow ? big : 0);
        if (step != 0) { Nudged?.Invoke(step); inputEvent.Handled = true; }
    }

    private int Axis(Position p) => _orientation == SplitOrientation.Horizontal ? p.X : p.Y;
    #endregion

    #region Fields
    private readonly SplitOrientation _orientation;
    private bool _dragging;
    private int _dragLast;
    private Style _lineStyle;
    private Style _hoverStyle;
    private Style _activeStyle;
    #endregion
}
