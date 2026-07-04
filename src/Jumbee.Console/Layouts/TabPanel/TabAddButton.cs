namespace Jumbee.Console;

using System;
using System.Collections.Generic;

using ConsoleGUI.Space;

using Spectre.Console;
using Spectre.Console.Rendering;

/// <summary>
/// The "+" new-tab button appended to a <see cref="TabPanel"/>'s bar when <see cref="TabPanel.ShowAddButton"/> is
/// set. Mouse-only (not focusable, and excluded from the panel's logical rows) so it never enters keyboard tab
/// traversal — clicking it raises <see cref="Clicked"/>, which the panel turns into <see cref="TabPanel.NewTabRequested"/>.
/// </summary>
internal sealed class TabAddButton : RenderableControl
{
    #region Constructors
    internal TabAddButton()
    {
        Focusable = false;   // mouse-only; keyboard "new tab" is a hotkey on the owning control
        ApplyTheme();
        Height = 1;
        Width = _glyph.GetCellWidth() + 2;   // a space of padding either side
    }
    #endregion

    #region Events
    /// <summary>Raised when the button is clicked.</summary>
    public event EventHandler? Clicked;
    #endregion

    #region Properties
    // Tag the button's cells with a mouse listener even though it isn't focusable, so it receives hover/click.
    protected override bool WantsMouse => true;
    #endregion

    #region Methods
    protected override void ApplyTheme()
    {
        _glyph = UI.GlyphTheme.TabAdd;
        _style = UI.StyleTheme.TextMuted;
        _hoverStyle = UI.StyleTheme.Hover;
    }

    protected override IEnumerable<Segment> Render(RenderOptions options, int maxWidth)
    {
        var style = (IsMouseOver ? _hoverStyle : _style).SpectreConsoleStyle;
        var label = $" {_glyph} ";
        if (label.Length < maxWidth) label = label.PadRight(maxWidth);
        else if (label.Length > maxWidth) label = label[..Math.Max(0, maxWidth)];
        yield return new Segment(label, style);
    }

    protected override void OnClick(Position position) => Clicked?.Invoke(this, EventArgs.Empty);
    #endregion

    #region Fields
    private string _glyph = "";
    private Style _style;
    private Style _hoverStyle;
    #endregion
}
