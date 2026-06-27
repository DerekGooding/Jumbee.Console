namespace Jumbee.Console;

using System;
using System.Collections.Generic;

using ConsoleGUI.Input;
using ConsoleGUI.Space;

using Spectre.Console;
using Spectre.Console.Rendering;

/// <summary>
/// A single clickable tab label in a <see cref="TabPanel"/>'s tab bar. Focusable and listener-tagging (so a click
/// selects it and keyboard focus can land on it); renders its name styled by active / hover / inactive state.
/// Selecting raises <see cref="Activated"/>; an arrow key along the bar raises <see cref="Navigated"/> with a step
/// of -1/+1. The owning <see cref="TabPanel"/> wires both.
/// </summary>
public class TabHeader : RenderableControl
{
    #region Constructors
    internal TabHeader(int index, string text)
    {
        _index = index;
        _text = text;
        ApplyTheme();        // capture SelectionStyle + caret first: a caret style reserves a gutter in the width
        Height = 1;
        Width = LabelWidth();
    }
    #endregion

    #region Events
    /// <summary>Raised when this tab is chosen — by a click or by Enter/Space while focused.</summary>
    public event EventHandler? Activated;
    #endregion

    #region Properties
    public override bool HandlesInput => true;

    /// <summary>This tab's position in the bar.</summary>
    public int Index => _index;

    /// <summary>The tab label.</summary>
    public string Text
    {
        get => _text;
        set => SetAtomicProperty(ref _text, value, updatesLayout: true, watch: (_, _) => Width = LabelWidth());
    }

    /// <summary><see langword="true"/> for the currently selected tab (drawn with <see cref="ActiveStyle"/>).</summary>
    public bool IsActive { get => _isActive; set => SetAtomicProperty(ref _isActive, value); }

    /// <summary>Style of the active (selected) tab. Defaults to <see cref="IStyleTheme.Selection"/>.</summary>
    public Style ActiveStyle { get => _activeStyle; set => SetAtomicProperty(ref _activeStyle, value, themeOverride: true); }

    /// <summary>Style of inactive tabs. Defaults to <see cref="IStyleTheme.TextMuted"/>.</summary>
    public Style InactiveStyle { get => _inactiveStyle; set => SetAtomicProperty(ref _inactiveStyle, value, themeOverride: true); }

    /// <summary>Style of a hovered inactive tab. Defaults to <see cref="IStyleTheme.Hover"/>.</summary>
    public Style HoverStyle { get => _hoverStyle; set => SetAtomicProperty(ref _hoverStyle, value, themeOverride: true); }

    /// <summary>How the active tab is indicated — highlight / underline / caret. Set by the owning
    /// <see cref="TabPanel"/>; defaults to the theme's <see cref="IStyleTheme.SelectionStyle"/>.</summary>
    internal SelectionStyle SelectionStyle { get => _selectionStyle; set => SetAtomicProperty(ref _selectionStyle, value, updatesLayout: true, themeOverride: true, watch: (_, _) => Width = LabelWidth()); }
    #endregion

    #region Methods
    protected override void ApplyTheme()
    {
        if (!IsThemeOverridden(nameof(ActiveStyle))) _activeStyle = UI.StyleTheme.Selection;
        if (!IsThemeOverridden(nameof(InactiveStyle))) _inactiveStyle = UI.StyleTheme.TextMuted;
        if (!IsThemeOverridden(nameof(HoverStyle))) _hoverStyle = UI.StyleTheme.Hover;
        if (!IsThemeOverridden(nameof(SelectionStyle))) _selectionStyle = UI.StyleTheme.SelectionStyle;
        _selectionCaret = UI.GlyphTheme.SelectionCaret;
    }

    protected override IEnumerable<Segment> Render(RenderOptions options, int maxWidth)
    {
        Spectre.Console.Style style;
        string label;
        // In Caret mode every header reserves a caret-width gutter (the active one shows the caret, the others blank)
        // so the labels stay aligned and the caret never truncates the text.
        var gutter = _selectionStyle == SelectionStyle.Caret ? new string(' ', _selectionCaret.GetCellWidth()) : "";
        if (_isActive)
        {
            // The active tab is the "selected item": render it per SelectionStyle (highlight / underline / caret).
            style = _selectionStyle.TextStyle(_activeStyle.ForegroundColor, _activeStyle.BackgroundColor);
            label = $" {(_selectionStyle == SelectionStyle.Caret ? _selectionCaret : gutter)}{_text} ";
        }
        else
        {
            var s = IsMouseOver ? _hoverStyle : _inactiveStyle;
            if (IsFocused) s |= Style.Underline;   // show keyboard focus on an inactive header
            style = s.SpectreConsoleStyle;
            label = $" {gutter}{_text} ";
        }

        if (label.Length < maxWidth) label = label.PadRight(maxWidth);
        else if (label.Length > maxWidth) label = label[..Math.Max(0, maxWidth)];

        yield return new Segment(label, style);
    }

    protected override void OnClick(Position position) => Activated?.Invoke(this, EventArgs.Empty);

    protected override void OnInput(InputEvent inputEvent)
    {
        // Enter/Space selects this tab; switching between tabs (Alt+arrows) is handled by the owning TabPanel.
        if (inputEvent.Key.Key is ConsoleKey.Enter or ConsoleKey.Spacebar)
        {
            Activated?.Invoke(this, EventArgs.Empty);
            inputEvent.Handled = true;
        }
    }

    // A space of padding either side, plus a caret-width gutter when the selection style is Caret.
    private int LabelWidth() => _text.Length + 2 + (_selectionStyle == SelectionStyle.Caret ? _selectionCaret.GetCellWidth() : 0);
    #endregion

    #region Fields
    private readonly int _index;
    private string _text;
    private bool _isActive;
    private Style _activeStyle;
    private Style _inactiveStyle;
    private Style _hoverStyle;
    private SelectionStyle _selectionStyle;
    private string _selectionCaret = "";
    #endregion
}
