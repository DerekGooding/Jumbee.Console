
using ConsoleGUI.Input;
using ConsoleGUI.Space;
using Spectre.Console.Rendering;

namespace Jumbee.Console;
/// <summary>
/// Shared base for the vertical, navigable toggle lists (<see cref="RadioSet"/>, <see cref="SelectionList"/>).
/// </summary>
/// <remarks>
/// <para>
/// Each option is one row rendered as a state indicator followed by the option text. Up/Down move the highlight
/// cursor (auto-scrolling the surrounding <see cref="Control.Frame"/>); Space/Enter activate the highlighted row
/// and a click activates the clicked row. Subclasses define the per-row checked state, the indicator glyph pair,
/// and what "activate" does (single- vs multi-select).
/// </para>
/// <para>
/// Appearance is theme-driven: style tokens are captured once from <see cref="UI.StyleTheme"/> in the constructor
/// and glyphs from <see cref="UI.GlyphTheme"/> via <see cref="SetGlyphs"/>; the indicator width (and the control's
/// width) is measured from the themed glyph. Captured values are read as plain fields on the render path.
/// </para>
/// </remarks>
public abstract class ToggleList : RenderableControl
{
    #region Constructors

    /// <summary>Initializes a new <see cref="ToggleList"/> with the given <paramref name="options"/>.</summary>
    protected ToggleList(IEnumerable<string> options)
    {
        _options = [.. options];
        Height = Math.Max(1, _options.Count);
        // Styles + glyphs are captured by ApplyTheme, which the subclass calls from its constructor (and which
        // re-runs on a runtime theme switch). Width is set there via SetGlyphs.
    }

    #endregion Constructors

    #region Properties

    /// <summary>Reports <see langword="true"/> so input routing delivers keys to the control.</summary>
    public override bool HandlesInput => true;

    /// <summary>The list's options.</summary>
    public IReadOnlyList<string> Options => _options;

    /// <summary>The highlighted row (navigation cursor), clamped to the option range.</summary>
    public int CursorIndex
    {
        get;
        set
        {
            var clamped = _options.Count == 0 ? 0 : Math.Clamp(value, 0, _options.Count - 1);
            if (clamped == field) return;
            field = clamped;
            AutoScroll();
            Invalidate();
        }
    }

    /// <summary>Text style for an option label. Defaults to <see cref="IStyleTheme.Text"/>.</summary>
    public Style TextStyle { get => _textStyle; set => SetAtomicProperty(ref _textStyle, value, themeOverride: true); }

    /// <summary>Indicator style for a checked/selected row. Defaults to <see cref="IStyleTheme.TextAccent"/>.</summary>
    public Style AccentStyle { get => _accentStyle; set => SetAtomicProperty(ref _accentStyle, value, themeOverride: true); }

    /// <summary>Indicator style for an unchecked row. Defaults to <see cref="IStyleTheme.TextMuted"/>.</summary>
    public Style MutedStyle { get => _mutedStyle; set => SetAtomicProperty(ref _mutedStyle, value, themeOverride: true); }

    /// <summary>Style for the highlighted (cursor) row. Defaults to <see cref="IStyleTheme.Selection"/>.</summary>
    public Style SelectionStyle
    {
        get => _selectionStyle;
        set => SetAtomicProperty(ref _selectionStyle, value, watch: (_, v) => _selectionBackground = SelectionBg(v), themeOverride: true);
    }

    #endregion Properties

    #region Methods

    /// <summary><see langword="true"/> if the option at <paramref name="index"/> is currently selected/checked.</summary>
    protected abstract bool IsChecked(int index);

    /// <summary>Acts on the option at <paramref name="index"/> (select it, or toggle its checked state).</summary>
    protected abstract void Activate(int index);

    /// <inheritdoc/>
    // Captures the row styles from the style theme. Subclasses override to also set their glyphs (via SetGlyphs)
    // from the glyph theme, calling base first.
    protected override void ApplyTheme()
    {
        if (!IsThemeOverridden(nameof(TextStyle))) _textStyle = UI.StyleTheme.Text;
        if (!IsThemeOverridden(nameof(AccentStyle))) _accentStyle = UI.StyleTheme.TextAccent;
        if (!IsThemeOverridden(nameof(MutedStyle))) _mutedStyle = UI.StyleTheme.TextMuted;
        if (!IsThemeOverridden(nameof(SelectionStyle)))
        {
            _selectionStyle = UI.StyleTheme.Selection;
            _selectionBackground = SelectionBg(_selectionStyle);
        }
    }

    /// <summary>Sets the selected/unselected indicator glyphs (from the glyph theme), measures the indicator
    /// width from them, and re-sizes the control. Subclasses call this from their constructor.</summary>
    protected void SetGlyphs(string on, string off)
    {
        _on = on;
        _off = off;
        _indicatorWidth = IGlyphTheme.CellWidth(on, off);
        Width = _indicatorWidth + 1 + (_options.Count == 0 ? 0 : _options.Max(o => o.Length));
    }

    /// <inheritdoc/>
    protected override void OnInput(InputEvent inputEvent)
    {
        var count = _options.Count;
        if (count == 0) return;

        switch (inputEvent.Key.Key)
        {
            case ConsoleKey.UpArrow:
                CursorIndex = (CursorIndex - 1 + count) % count;
                inputEvent.Handled = true;
                break;

            case ConsoleKey.DownArrow:
                CursorIndex = (CursorIndex + 1) % count;
                inputEvent.Handled = true;
                break;

            case ConsoleKey.Home:
                CursorIndex = 0;
                inputEvent.Handled = true;
                break;

            case ConsoleKey.End:
                CursorIndex = count - 1;
                inputEvent.Handled = true;
                break;

            case ConsoleKey.Enter:
            case ConsoleKey.Spacebar:
                Activate(CursorIndex);
                inputEvent.Handled = true;
                break;
        }
    }

    /// <inheritdoc/>
    // Each option is one row and the listener position is in content coordinates, so the row index is position.Y.
    protected override void OnClick(Position position)
    {
        var index = position.Y;
        if (index < 0 || index >= _options.Count) return;
        CursorIndex = index;
        Activate(index);
    }

    /// <inheritdoc/>
    // A double-click is two presses; treat it as clicking the row twice.
    protected override void OnDoubleClick(Position position) => OnClick(position);

    /// <inheritdoc/>
    protected override IEnumerable<Segment> Render(RenderOptions options, int maxWidth)
    {
        for (var i = 0; i < _options.Count; i++)
        {
            var highlighted = i == CursorIndex;
            // The cursor row takes the selection foreground/background for its label and overlays the selection
            // background on the indicator, keeping the indicator's accent/muted hue.
            var indicator = IsChecked(i) ? _accentStyle : _mutedStyle;
            var label = highlighted ? _selectionStyle : _textStyle;
            if (highlighted) indicator |= _selectionBackground;

            yield return new Segment(IsChecked(i) ? _on : _off, indicator);

            var text = " " + _options[i];
            var fill = Math.Max(0, maxWidth - _indicatorWidth);
            text = text.Length > fill ? text[..fill] : text.PadRight(fill);
            yield return new Segment(text, label);

            if (i < _options.Count - 1) yield return Segment.LineBreak;
        }
    }

    // The highlighted row overlays only the selection background on each indicator (keeping its hue); extract
    // that background, or use a no-op style when the selection token carries no background.
    private static Style SelectionBg(Style selection) =>
        selection.BackgroundColor is { } bg ? Style.Bg(bg) : Style.Plain;

    private void AutoScroll()
    {
        if (Frame == null) return;

        var y = CursorIndex;
        var top = Frame.Top;
        var viewportHeight = Frame.ViewportSize.Height;
        if (viewportHeight <= 0) return;

        if (y < top) Frame.Top = y;
        else if (y >= top + viewportHeight) Frame.Top = y - viewportHeight + 1;
    }

    #endregion Methods

    #region Fields

    protected readonly List<string> _options;
    private string _on = "";
    private string _off = "";
    private int _indicatorWidth;
    private Style _textStyle;
    private Style _accentStyle;
    private Style _mutedStyle;
    private Style _selectionStyle;
    private Style _selectionBackground;

    #endregion Fields
}