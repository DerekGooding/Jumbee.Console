namespace Jumbee.Console;

using Spectre.Console.Rendering;

/// <summary>
/// A sliding on/off switch. Renders a small track with the thumb on the right and tinted with <see cref="ToggleButton.Accent"/>
/// when on (<c>(  ●)</c>), and on the left tinted with <see cref="ToggleButton.Muted"/> when off (<c>(●  )</c>),
/// followed by its label.
/// </summary>
public class Switch : ToggleButton
{
    #region Constructors
    public Switch(string text = "", bool isOn = false) : base(text) => IsChecked = isOn;
    #endregion

    #region Methods
    protected override int IndicatorWidth => 4;

    protected override Segment RenderIndicator(Color? background)
    {
        var glyph = IsChecked ? "(─●)" : "(●─)";
        return new Segment(glyph, Style(IsChecked ? Accent : Muted, background));
    }
    #endregion
}
