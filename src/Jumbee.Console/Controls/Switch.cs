namespace Jumbee.Console;

/// <summary>
/// A sliding on/off switch. Uses the glyph theme's <see cref="IGlyphTheme.SwitchOn"/>/
/// <see cref="IGlyphTheme.SwitchOff"/> markers (by default <c>(─●)</c>/<c>(●─)</c>), tinted with the accent
/// style when on, followed by its label.
/// </summary>
public class Switch : ToggleButton
{
    #region Constructors
    public Switch(string text = "", bool isOn = false) : base(text)
    {
        SetGlyphs(UI.GlyphTheme.SwitchOn, UI.GlyphTheme.SwitchOff);
        IsChecked = isOn;
    }
    #endregion
}
