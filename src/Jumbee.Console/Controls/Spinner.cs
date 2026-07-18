namespace Jumbee.Console;

using System;
using System.Linq;

using Spectre.Console;

/// <summary>An animated spinner glyph with an optional label, cycling through a <see cref="Spectre.Console.Spinner"/>'s frames.</summary>
public class Spinner : AnimatedControl
{
    #region Constructors
    /// <summary>Initializes a new <see cref="Spinner"/> using the default spinner style.</summary>
    public Spinner() => Focusable = false;   // a passive display control: never a focus/tab target
    #endregion

    #region Properties
    /// <summary>The spinner animation (frame set and interval) to cycle through.</summary>
    public Spectre.Console.Spinner SpinnerType
    {
        get => _spinner;
        set
        {
            _spinner = value;
            frameCount = _spinner.Frames.Count;
            interval = _spinner.Interval.Ticks;
            spinnerFrames = _spinner.Frames.Select(Style.EscapeMarkup).ToArray();
            spinnerFramesMarkup = spinnerFrames.Map(f => $"[{styleMarkup}]{f}[/]" + (string.IsNullOrEmpty(_text) ? "" : " " + _text));
        }
    }

    /// <summary>The style applied to the spinner glyph and label.</summary>
    public Style Style
    {
        get => _style;
        set
        {
            _style = value;
            styleMarkup = _style;
            spinnerFramesMarkup = spinnerFrames.Map(f => $"[{styleMarkup}]{f}[/]" + (string.IsNullOrEmpty(_text) ? "" : " " + _text));
        }
    }

    /// <summary>An optional label drawn after the spinner glyph.</summary>
    public string Text
    {
        get => _text;
        set
        {
            _text = Markup.Escape(value);
            spinnerFramesMarkup = spinnerFrames.Map(f => $"[{styleMarkup}]{f}[/]" + (string.IsNullOrEmpty(_text) ? "" : " " + _text));
        }
    }
    #endregion

    #region Methods
    /// <summary>Renders the current animation frame (glyph plus label).</summary>
    protected sealed override void Render()
    {
        ansiConsole.Clear(true);        
        ansiConsole.Markup(spinnerFramesMarkup[frameIndex % spinnerFrames.Length]);        
    }
    #endregion

    #region Fields
    private Spectre.Console.Spinner _spinner = Spectre.Console.Spinner.Known.Default;
    private Style _style = Style.Plain;
    private string styleMarkup = Style.Plain;
    private string[] spinnerFrames = Spectre.Console.Spinner.Known.Default.Frames.Select(Markup.Escape).ToArray();
    private string[] spinnerFramesMarkup = Spectre.Console.Spinner.Known.Default.Frames.Select(f => $"[{Style.Plain}]{f}[/]").ToArray();
    private string _text = string.Empty;
    #endregion
}
