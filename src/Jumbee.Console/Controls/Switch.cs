namespace Jumbee.Console;

/// <summary>
/// A sliding on/off switch.
/// </summary>
/// <remarks>
/// Uses the glyph theme's <see cref="IGlyphTheme.SwitchOn"/>/
/// <see cref="IGlyphTheme.SwitchOff"/> markers (by default <c>(─●)</c>/<c>(●─)</c>), tinted with the accent
/// style when on, followed by its label.
/// </remarks>
public class Switch : ToggleButton
{
    #region Constructors
    public Switch(string text = "", bool isOn = false) : base(text)
    {
        ApplyTheme();
        IsChecked = isOn;
    }
    #endregion

    #region Methods
    protected override void ApplyTheme()
    {
        base.ApplyTheme();
        SetGlyphs(UI.GlyphTheme.SwitchOn, UI.GlyphTheme.SwitchOff);
    }
    #endregion
}
