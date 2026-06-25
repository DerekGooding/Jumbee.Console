namespace Jumbee.Console;

/// <summary>
/// A labelled checkbox that toggles an independent on/off state. Uses the glyph theme's
/// <see cref="IGlyphTheme.CheckboxChecked"/>/<see cref="IGlyphTheme.CheckboxUnchecked"/> markers (by default
/// <c>[X]</c>/<c>[ ]</c>), followed by its label.
/// </summary>
public class Checkbox : ToggleButton
{
    #region Constructors
    public Checkbox(string text = "", bool isChecked = false) : base(text)
    {
        ApplyTheme();
        IsChecked = isChecked;
    }
    #endregion

    #region Methods
    protected override void ApplyTheme()
    {
        base.ApplyTheme();
        SetGlyphs(UI.GlyphTheme.CheckboxChecked, UI.GlyphTheme.CheckboxUnchecked);
    }
    #endregion
}
