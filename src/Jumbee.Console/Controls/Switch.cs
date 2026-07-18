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
    /// <summary>Initializes a new <see cref="Switch"/> with the given label <paramref name="text"/> and initial <paramref name="isOn"/> state.</summary>
    public Switch(string text = "", bool isOn = false) : base(text)
    {
        ApplyTheme();
        IsChecked = isOn;
    }
    #endregion

    #region Methods
    /// <inheritdoc/>
    protected override void ApplyTheme()
    {
        base.ApplyTheme();
        SetGlyphs(UI.GlyphTheme.SwitchOn, UI.GlyphTheme.SwitchOff);
    }
    #endregion
}
