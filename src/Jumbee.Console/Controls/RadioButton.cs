namespace Jumbee.Console;

/// <summary>
/// A labelled radio button.
/// </summary>
/// <remarks>
/// Uses the glyph theme's <see cref="IGlyphTheme.RadioSelected"/>/
/// <see cref="IGlyphTheme.RadioUnselected"/> markers (by default <c>(●)</c>/<c>( )</c>). Unlike a
/// <see cref="Checkbox"/>, activating a radio button latches it on rather than toggling; mutual exclusion
/// between several radio buttons is provided by a <see cref="RadioSet"/>.
/// </remarks>
public class RadioButton : ToggleButton
{
    #region Constructors

    /// <summary>Initializes a new <see cref="RadioButton"/> with the given label <paramref name="text"/> and initial <paramref name="isChecked"/> state.</summary>
    public RadioButton(string text = "", bool isChecked = false) : base(text)
    {
        ApplyTheme();
        IsChecked = isChecked;
    }

    #endregion Constructors

    #region Methods

    /// <summary>Latches the button on (a click never turns a radio button off).</summary>
    public override void Toggle() => IsChecked = true;

    /// <inheritdoc/>
    protected override void ApplyTheme()
    {
        base.ApplyTheme();
        SetGlyphs(UI.GlyphTheme.RadioSelected, UI.GlyphTheme.RadioUnselected);
    }

    #endregion Methods
}