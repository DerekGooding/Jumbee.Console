namespace Jumbee.Console;

/// <summary>
/// A labelled checkbox that toggles an independent on/off state.
/// </summary>
/// <remarks>
/// Uses the glyph theme's <see cref="IGlyphTheme.CheckboxChecked"/>/<see cref="IGlyphTheme.CheckboxUnchecked"/>
/// markers (by default <c>[X]</c>/<c>[ ]</c>), followed by its label.
/// </remarks>
public class Checkbox : ToggleButton
{
    #region Constructors
    /// <summary>Initializes a new <see cref="Checkbox"/> with the given label and initial checked state.</summary>
    public Checkbox(string text = "", bool isChecked = false) : base(text)
    {
        ApplyTheme();
        IsChecked = isChecked;
    }
    #endregion

    #region Methods
    /// <inheritdoc/>
    protected override void ApplyTheme()
    {
        base.ApplyTheme();
        SetGlyphs(UI.GlyphTheme.CheckboxChecked, UI.GlyphTheme.CheckboxUnchecked);
    }
    #endregion
}
