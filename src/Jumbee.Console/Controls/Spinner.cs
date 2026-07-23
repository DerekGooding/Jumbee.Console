
using Spectre.Console;

namespace Jumbee.Console;
/// <summary>An animated spinner glyph with an optional label, cycling through a <see cref="Spectre.Console.Spinner"/>'s frames.</summary>
public class Spinner : AnimatedControl
{
    #region Constructors

    /// <summary>Initializes a new <see cref="Spinner"/> using the default spinner style.</summary>
    public Spinner() => Focusable = false;   // a passive display control: never a focus/tab target

    #endregion Constructors

    #region Properties

    /// <summary>The spinner animation (frame set and interval) to cycle through.</summary>
    public Spectre.Console.Spinner SpinnerType
    {
        get;
        set
        {
            field = value;
            frameCount = field.Frames.Count;
            interval = field.Interval.Ticks;
            spinnerFrames = [.. field.Frames.Select(Style.EscapeMarkup)];
            spinnerFramesMarkup = spinnerFrames.Map(f => $"[{styleMarkup}]{f}[/]" + (string.IsNullOrEmpty(Text) ? "" : " " + Text));
        }
    } = Spectre.Console.Spinner.Known.Default;

    /// <summary>The style applied to the spinner glyph and label.</summary>
    public Style Style
    {
        get;
        set
        {
            field = value;
            styleMarkup = field;
            spinnerFramesMarkup = spinnerFrames.Map(f => $"[{styleMarkup}]{f}[/]" + (string.IsNullOrEmpty(Text) ? "" : " " + Text));
        }
    } = Style.Plain;

    /// <summary>An optional label drawn after the spinner glyph.</summary>
    public string Text
    {
        get;
        set
        {
            field = Markup.Escape(value);
            spinnerFramesMarkup = spinnerFrames.Map(f => $"[{styleMarkup}]{f}[/]" + (string.IsNullOrEmpty(field) ? "" : " " + field));
        }
    } = string.Empty;

    #endregion Properties

    #region Methods

    /// <summary>Renders the current animation frame (glyph plus label).</summary>
    protected override sealed void Render()
    {
        ansiConsole.Clear(true);
        ansiConsole.Markup(spinnerFramesMarkup[frameIndex % spinnerFrames.Length]);
    }

    #endregion Methods

    #region Fields

    private string styleMarkup = Style.Plain;
    private string[] spinnerFrames = [.. Spectre.Console.Spinner.Known.Default.Frames.Select(Markup.Escape)];
    private string[] spinnerFramesMarkup = [.. Spectre.Console.Spinner.Known.Default.Frames.Select(f => $"[{Style.Plain}]{f}[/]")];

    #endregion Fields
}