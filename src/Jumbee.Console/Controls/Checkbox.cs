namespace Jumbee.Console;

using Spectre.Console.Rendering;

/// <summary>
/// A labelled checkbox that toggles an independent on/off state. Renders <c>[X]</c> when checked and
/// <c>[ ]</c> when unchecked, followed by its label.
/// </summary>
public class Checkbox : ToggleButton
{
    #region Constructors
    public Checkbox(string text = "", bool isChecked = false) : base(text) => IsChecked = isChecked;
    #endregion

    #region Methods
    protected override int IndicatorWidth => 3;

    protected override Segment RenderIndicator(Color? background)
    {
        var glyph = IsChecked ? "[X]" : "[ ]";
        return new Segment(glyph, Style(IsChecked ? Accent : Muted, background));
    }
    #endregion
}
