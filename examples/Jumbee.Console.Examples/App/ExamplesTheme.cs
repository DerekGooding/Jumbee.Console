namespace Jumbee.Console.Examples;

/// <summary>
/// The examples app's theme — identical to the default, except keyboard focus is shown as a <em>border</em> that
/// appears on the focused control (via <see cref="IStyleTheme.FocusedFrameBorder"/>) rather than the default full
/// background tint. The panes already reserve border space (their frames are borderless at rest), so the border
/// appears on focus with no layout shift, and the tint is automatically suppressed.
/// </summary>
public sealed class ExamplesTheme : IStyleTheme
{
    public BorderStyle? FocusedFrameBorder => BorderStyle.Rounded;

    public TitleStyle TitleStyle { get; } = new TitleStyle(TitlePos.TopLeft, TitleBorderStyle.Inline); 
}
