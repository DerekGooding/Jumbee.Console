namespace Jumbee.Console;

/// <summary>
/// The per-part <see cref="Style"/> (foreground/background/decoration, no glyph) a control frame applies to its
/// vertical scrollbar.
/// </summary>
/// <remarks>The glyphs come separately from <see cref="ScrollBarGlyphs"/> (via
/// <see cref="IGlyphTheme.ScrollBar"/>); a control frame composes the two into its scrollbar cells.</remarks>
/// <remarks>Initializes a new <see cref="ScrollBarStyle"/> from the thumb, track, and end-arrow styles.</remarks>
public readonly struct ScrollBarStyle(Style thumb, Style track, Style upArrow, Style downArrow) : IEquatable<ScrollBarStyle>
{
    #region Properties

    /// <summary>Style for the thumb (the draggable handle).</summary>
    public Style Thumb { get; init; } = thumb;

    /// <summary>Style for the track behind the thumb.</summary>
    public Style Track { get; init; } = track;

    /// <summary>Style for the top end arrow.</summary>
    public Style UpArrow { get; init; } = upArrow;

    /// <summary>Style for the bottom end arrow.</summary>
    public Style DownArrow { get; init; } = downArrow;

    #endregion Properties

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

    #endregion Methods

    #region Presets

    /// <summary>The default colours: a medium-grey thumb on a dim dark-grey track (a neutral, modern groove that
    /// reads clearly under the smooth block bar), with terminal-default arrows for the classic bar.</summary>
    public static ScrollBarStyle Default { get; } = new(
        thumb: new Color(158, 158, 158),
        track: new Color(68, 68, 68),
        upArrow: Style.Plain,
        downArrow: Style.Plain);

    #endregion Presets

    #region Equality

    // Hand-written: Style holds a reference, so the runtime's default ValueType.Equals falls back to a reflective,
    // boxing field-by-field compare — which is what SetAtomicProperty would run on every assignment. See Color.
    /// <summary>Determines whether this <see cref="ScrollBarStyle"/> equals <paramref name="other"/>.</summary>
    public bool Equals(ScrollBarStyle other) =>
        Thumb == other.Thumb && Track == other.Track && UpArrow == other.UpArrow && DownArrow == other.DownArrow;

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is ScrollBarStyle other && Equals(other);

    /// <inheritdoc/>
    public override int GetHashCode() => System.HashCode.Combine(Thumb, Track, UpArrow, DownArrow);

    /// <summary>Equality operator.</summary>
    public static bool operator ==(ScrollBarStyle a, ScrollBarStyle b) => a.Equals(b);

    /// <summary>Inequality operator.</summary>
    public static bool operator !=(ScrollBarStyle a, ScrollBarStyle b) => !a.Equals(b);

    #endregion Equality
}