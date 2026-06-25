namespace Jumbee.Console;

/// <summary>
/// The per-part <see cref="Style"/> (foreground/background/decoration, no glyph) a control frame applies to its
/// vertical scrollbar. The glyphs come separately from <see cref="ScrollBarGlyphs"/> (via
/// <see cref="IGlyphTheme.ScrollBar"/>); a control frame composes the two into its scrollbar cells.
/// </summary>
public readonly struct ScrollBarStyle
{
    #region Constructors
    public ScrollBarStyle(Style thumb, Style track, Style upArrow, Style downArrow)
    {
        Thumb = thumb;
        Track = track;
        UpArrow = upArrow;
        DownArrow = downArrow;
    }
    #endregion

    #region Properties
    /// <summary>Style for the thumb (the draggable handle).</summary>
    public Style Thumb { get; init; }

    /// <summary>Style for the track behind the thumb.</summary>
    public Style Track { get; init; }

    /// <summary>Style for the top end arrow.</summary>
    public Style UpArrow { get; init; }

    /// <summary>Style for the bottom end arrow.</summary>
    public Style DownArrow { get; init; }
    #endregion

    #region Methods
    /// <summary>
    /// Returns a copy with the foreground colours overridden. A <c>null</c> argument leaves that part unchanged;
    /// <paramref name="arrows"/> recolours both end arrows.
    /// </summary>
    public ScrollBarStyle WithColors(Color? thumb = null, Color? track = null, Color? arrows = null) =>
        new(
            thumb is { } t ? (Style)t : Thumb,
            track is { } r ? (Style)r : Track,
            arrows is { } a ? (Style)a : UpArrow,
            arrows is { } d ? (Style)d : DownArrow);

    /// <summary>A scrollbar style with the same <see cref="Style"/> applied to every part.</summary>
    public static ScrollBarStyle Uniform(Style style) => new(style, style, style, style);
    #endregion

    #region Presets
    /// <summary>The original default colours: a blue thumb on a grey track with terminal-default arrows.</summary>
    public static ScrollBarStyle Default { get; } = new(
        thumb: new Color(100, 100, 255),
        track: new Color(100, 100, 100),
        upArrow: Style.Plain,
        downArrow: Style.Plain);
    #endregion
}
