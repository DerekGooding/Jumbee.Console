namespace Jumbee.Console;

using Spectre.Console.Rendering;

/// <summary>
/// A labelled radio button. Renders <c>(●)</c> when selected and <c>( )</c> otherwise. Unlike a
/// <see cref="Checkbox"/>, activating a radio button latches it on rather than toggling; mutual exclusion
/// between several radio buttons is provided by a <see cref="RadioSet"/>.
/// </summary>
public class RadioButton : ToggleButton
{
    #region Constructors
    public RadioButton(string text = "", bool isChecked = false) : base(text) => IsChecked = isChecked;
    #endregion

    #region Methods
    /// <summary>Latches the button on (a click never turns a radio button off).</summary>
    public override void Toggle() => IsChecked = true;

    protected override int IndicatorWidth => 3;

    protected override Segment RenderIndicator(Color? background)
    {
        var glyph = IsChecked ? "(●)" : "( )";
        return new Segment(glyph, Style(IsChecked ? Accent : Muted, background));
    }
    #endregion
}
