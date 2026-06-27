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
        Height = 1;
        Width = LabelWidth(text);
        ApplyTheme();
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
        set => SetAtomicProperty(ref _text, value, updatesLayout: true, watch: (_, _) => Width = LabelWidth(_text));
    }

    /// <summary><see langword="true"/> for the currently selected tab (drawn with <see cref="ActiveStyle"/>).</summary>
    public bool IsActive { get => _isActive; set => SetAtomicProperty(ref _isActive, value); }

    /// <summary>Style of the active (selected) tab. Defaults to <see cref="IStyleTheme.Selection"/>.</summary>
    public Style ActiveStyle { get => _activeStyle; set => SetAtomicProperty(ref _activeStyle, value, themeOverride: true); }

    /// <summary>Style of inactive tabs. Defaults to <see cref="IStyleTheme.TextMuted"/>.</summary>
    public Style InactiveStyle { get => _inactiveStyle; set => SetAtomicProperty(ref _inactiveStyle, value, themeOverride: true); }

    /// <summary>Style of a hovered inactive tab. Defaults to <see cref="IStyleTheme.Hover"/>.</summary>
    public Style HoverStyle { get => _hoverStyle; set => SetAtomicProperty(ref _hoverStyle, value, themeOverride: true); }
    #endregion

    #region Methods
    protected override void ApplyTheme()
    {
        if (!IsThemeOverridden(nameof(ActiveStyle))) _activeStyle = UI.StyleTheme.Selection;
        if (!IsThemeOverridden(nameof(InactiveStyle))) _inactiveStyle = UI.StyleTheme.TextMuted;
        if (!IsThemeOverridden(nameof(HoverStyle))) _hoverStyle = UI.StyleTheme.Hover;
    }

    protected override IEnumerable<Segment> Render(RenderOptions options, int maxWidth)
    {
        var style = _isActive ? _activeStyle : IsMouseOver ? _hoverStyle : _inactiveStyle;
        // Show keyboard focus that isn't already implied by the active highlight with an underline.
        if (IsFocused && !_isActive) style |= Style.Underline;

        var label = $" {_text} ";
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

    private static int LabelWidth(string text) => text.Length + 2;   // a space of padding either side
    #endregion

    #region Fields
    private readonly int _index;
    private string _text;
    private bool _isActive;
    private Style _activeStyle;
    private Style _inactiveStyle;
    private Style _hoverStyle;
    #endregion
}
