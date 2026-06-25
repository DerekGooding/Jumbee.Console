namespace Jumbee.Console;

using System;
using System.Collections.Generic;

using ConsoleGUI.Input;
using ConsoleGUI.Space;

using Spectre.Console.Rendering;

/// <summary>
/// Shared base for the single-state toggle widgets (<see cref="Checkbox"/>, <see cref="RadioButton"/>,
/// <see cref="Switch"/>). Renders a state indicator followed by an optional text label, toggles on a mouse
/// click or Enter/Space while focused, and raises <see cref="Changed"/> when the state flips.
/// <para>
/// Appearance is theme-driven: the style tokens are captured once from <see cref="UI.StyleTheme"/> in the
/// constructor, and each subclass calls <see cref="SetGlyphs"/> with its <see cref="UI.GlyphTheme"/> glyphs.
/// The indicator width (and hence the control's width) is derived from the themed glyph, so a glyph theme with
/// wider/narrower markers re-sizes the control without any control-specific code. Captured tokens are read on
/// the render path as plain fields, so theming costs nothing per frame; callers may still override any token
/// via its setter.
/// </para>
/// </summary>
public abstract class ToggleButton : RenderableControl
{
    #region Constructors
    protected ToggleButton(string text)
    {
        _text = text;
        _labelStyle = UI.StyleTheme.Text;
        _accentStyle = UI.StyleTheme.TextAccent;
        _mutedStyle = UI.StyleTheme.TextMuted;
        _hoverStyle = UI.StyleTheme.Hover;
        Height = 1;
        // Width is set by the subclass via SetGlyphs, once its themed glyphs (and their measured width) are known.
    }
    #endregion

    #region Events
    /// <summary>Raised with the new state whenever <see cref="IsChecked"/> changes.</summary>
    public event EventHandler<bool>? Changed;
    #endregion

    #region Properties
    public override bool HandlesInput => true;

    public bool IsChecked
    {
        get => _isChecked;
        set => SetAtomicProperty(ref _isChecked, value, watch: (_, v) => Changed?.Invoke(this, v));
    }

    public string Text
    {
        get => _text;
        set => SetAtomicProperty(ref _text, value, updatesLayout: true, watch: (_, _) => RefreshWidth());
    }

    /// <summary>Label style. Defaults to <see cref="IStyleTheme.Text"/>.</summary>
    public Style LabelStyle { get => _labelStyle; set => SetAtomicProperty(ref _labelStyle, value); }

    /// <summary>Indicator style when checked. Defaults to <see cref="IStyleTheme.TextAccent"/>.</summary>
    public Style AccentStyle { get => _accentStyle; set => SetAtomicProperty(ref _accentStyle, value); }

    /// <summary>Indicator style when unchecked. Defaults to <see cref="IStyleTheme.TextMuted"/>.</summary>
    public Style MutedStyle { get => _mutedStyle; set => SetAtomicProperty(ref _mutedStyle, value); }

    /// <summary>Style merged across the row while hovered (typically a background). Defaults to <see cref="IStyleTheme.Hover"/>.</summary>
    public Style HoverStyle { get => _hoverStyle; set => SetAtomicProperty(ref _hoverStyle, value); }
    #endregion

    #region Methods
    /// <summary>Flips the state (the same path as a click). Overridden by <see cref="RadioButton"/> to latch on.</summary>
    public virtual void Toggle() => IsChecked = !IsChecked;

    /// <summary>Sets the on/off indicator glyphs (from the glyph theme), measures the indicator width from them,
    /// and re-sizes the control. Subclasses call this from their constructor.</summary>
    protected void SetGlyphs(string on, string off)
    {
        _on = on;
        _off = off;
        _indicatorWidth = IGlyphTheme.CellWidth(on, off);
        RefreshWidth();
    }

    protected int IndicatorWidth => _indicatorWidth;

    protected override IEnumerable<Segment> Render(RenderOptions options, int maxWidth)
    {
        var indicator = _isChecked ? _accentStyle : _mutedStyle;
        var label = _labelStyle;
        if (IsMouseOver) { indicator |= _hoverStyle; label |= _hoverStyle; }

        yield return new Segment(_isChecked ? _on : _off, indicator);

        var text = _text.Length > 0 ? " " + _text : string.Empty;
        var fill = Math.Max(0, maxWidth - _indicatorWidth);
        text = text.Length > fill ? text[..fill] : text.PadRight(fill);
        yield return new Segment(text, label);
    }

    protected override void OnClick(Position position) => Toggle();

    // A double-click is two presses; for a toggle that means two state changes (the same as clicking twice).
    protected override void OnDoubleClick(Position position) => Toggle();

    protected override void OnInput(InputEvent inputEvent)
    {
        if (inputEvent.Key.Key is ConsoleKey.Enter or ConsoleKey.Spacebar)
        {
            Toggle();
            inputEvent.Handled = true;
        }
    }

    private void RefreshWidth() => Width = _indicatorWidth + (_text.Length > 0 ? _text.Length + 1 : 0);
    #endregion

    #region Fields
    private bool _isChecked;
    private string _text;
    private string _on = "";
    private string _off = "";
    private int _indicatorWidth;
    private Style _labelStyle;
    private Style _accentStyle;
    private Style _mutedStyle;
    private Style _hoverStyle;
    #endregion
}
