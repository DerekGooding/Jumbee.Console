namespace Jumbee.Console;

/// <summary>
/// A labelled radio button. Uses the glyph theme's <see cref="IGlyphTheme.RadioSelected"/>/
/// <see cref="IGlyphTheme.RadioUnselected"/> markers (by default <c>(●)</c>/<c>( )</c>). Unlike a
/// <see cref="Checkbox"/>, activating a radio button latches it on rather than toggling; mutual exclusion
/// between several radio buttons is provided by a <see cref="RadioSet"/>.
/// </summary>
public class RadioButton : ToggleButton
{
    #region Constructors
    public RadioButton(string text = "", bool isChecked = false) : base(text)
    {
        SetGlyphs(UI.GlyphTheme.RadioSelected, UI.GlyphTheme.RadioUnselected);
        IsChecked = isChecked;
    }
    #endregion

    #region Methods
    /// <summary>Latches the button on (a click never turns a radio button off).</summary>
    public override void Toggle() => IsChecked = true;
    #endregion
}
