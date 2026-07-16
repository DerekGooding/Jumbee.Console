namespace Jumbee.Console;

/// <summary>How a control frame renders its vertical scrollbar.</summary>
public enum ScrollBarMode
{
    /// <summary>Modern-style bar: a solid thumb over a solid track whose ends render at <em>sub-cell</em>
    /// resolution using eighth-block glyphs (<c>▁▂▃▄▅▆▇█</c>) so the thumb glides smoothly rather than snapping to
    /// whole rows. No end arrows; the thumb spans the whole column. Assumes a terminal with block-glyph support
    /// (Windows Terminal and most modern emulators). The thumb/track <em>glyphs</em> in this struct are ignored in
    /// this mode (the bar computes its own); only the <see cref="ScrollBarStyle"/> colours are used.</summary>
    Smooth,

    /// <summary>The classic three-part bar: the fixed <see cref="ScrollBarGlyphs.Thumb"/>/<see cref="ScrollBarGlyphs.Track"/>
    /// glyphs and <see cref="ScrollBarGlyphs.UpArrow"/>/<see cref="ScrollBarGlyphs.DownArrow"/> end arrows, positioned
    /// in whole cells. Portable to legacy terminals that lack eighth-block glyphs.</summary>
    Classic,
}

/// <summary>
/// The glyphs (no colours) a control frame's vertical scrollbar draws for each part: the moving thumb, the track
/// behind it, and the two end arrows. Colours/decorations come separately from <see cref="ScrollBarStyle"/> (via
/// <see cref="IStyleTheme.ScrollBar"/>); a control frame composes the two into its scrollbar cells. The static
/// presets are convenience helpers for restyling a single control's glyphs (e.g. via WithScrollBarGlyphs).
/// <para><see cref="Mode"/> selects the rendering: <see cref="ScrollBarMode.Smooth"/> (the default) ignores the
/// glyph strings and draws a sub-cell block bar; <see cref="ScrollBarMode.Classic"/> uses the four glyphs below.</para>
/// </summary>
public readonly struct ScrollBarGlyphs : System.IEquatable<ScrollBarGlyphs>
{
    #region Constructors
    /// <summary>Builds a <see cref="ScrollBarMode.Classic"/> glyph set (explicit glyphs imply the classic bar).</summary>
    public ScrollBarGlyphs(string thumb, string track, string upArrow, string downArrow)
    {
        Thumb = thumb;
        Track = track;
        UpArrow = upArrow;
        DownArrow = downArrow;
        Mode = ScrollBarMode.Classic;
    }
    #endregion

    #region Properties
    /// <summary>Which bar to render (smooth sub-cell block bar, or the classic three-part bar). Defaults to
    /// <see cref="ScrollBarMode.Smooth"/> for a default-constructed value and for the <see cref="Smooth"/> preset.</summary>
    public ScrollBarMode Mode { get; init; }

    /// <summary>The glyph for the part of the track currently in view (the draggable handle). Classic mode only.</summary>
    public string Thumb { get; init; }

    /// <summary>The glyph for the track behind the thumb. Classic mode only.</summary>
    public string Track { get; init; }

    /// <summary>The glyph at the top end of the scrollbar. Classic mode only.</summary>
    public string UpArrow { get; init; }

    /// <summary>The glyph at the bottom end of the scrollbar. Classic mode only.</summary>
    public string DownArrow { get; init; }
    #endregion

    #region Presets
    /// <summary>The default: the modern <see cref="ScrollBarMode.Smooth"/> block bar (sub-cell thumb, no arrows).</summary>
    public static ScrollBarGlyphs Default { get; } = Smooth;

    /// <summary>The modern <see cref="ScrollBarMode.Smooth"/> block bar. The glyph fields are placeholders (a solid
    /// block) and unused by the smooth renderer, which draws its own eighth-block cells.</summary>
    public static ScrollBarGlyphs Smooth { get; } =
        new() { Mode = ScrollBarMode.Smooth, Thumb = "█", Track = "█", UpArrow = "", DownArrow = "" };

    /// <summary>The original legacy glyphs for terminals without block support: a '#' thumb on a '|' track with
    /// triangle arrows (<see cref="ScrollBarMode.Classic"/>).</summary>
    public static ScrollBarGlyphs Classic { get; } = new("#", "|", "▲", "▼");

    /// <summary>A solid block thumb on a light vertical-line track with triangle arrows (classic).</summary>
    public static ScrollBarGlyphs Block { get; } = new("█", "│", "▲", "▼");

    /// <summary>A shaded (dithered) thumb on a light vertical-line track with thin line arrows (classic).</summary>
    public static ScrollBarGlyphs Shaded { get; } = new("▒", "│", "↑", "↓");

    /// <summary>A heavy vertical-line thumb on a light vertical-line track with triangle arrows (classic).</summary>
    public static ScrollBarGlyphs Line { get; } = new("┃", "│", "▲", "▼");
    #endregion

    #region Equality
    // Hand-written: the string fields make this struct non-bitwise-comparable, so the runtime's default
    // ValueType.Equals falls back to a reflective, boxing compare (~336 bytes per comparison). See Color.
    // Ordinal string comparison — these are glyphs, not text, so culture must not enter into it.
    public bool Equals(ScrollBarGlyphs other) =>
        Mode == other.Mode
        && string.Equals(Thumb, other.Thumb, System.StringComparison.Ordinal)
        && string.Equals(Track, other.Track, System.StringComparison.Ordinal)
        && string.Equals(UpArrow, other.UpArrow, System.StringComparison.Ordinal)
        && string.Equals(DownArrow, other.DownArrow, System.StringComparison.Ordinal);

    public override bool Equals(object? obj) => obj is ScrollBarGlyphs other && Equals(other);

    public override int GetHashCode()
    {
        var hash = new System.HashCode();
        hash.Add(Mode);
        hash.Add(Thumb, System.StringComparer.Ordinal);
        hash.Add(Track, System.StringComparer.Ordinal);
        hash.Add(UpArrow, System.StringComparer.Ordinal);
        hash.Add(DownArrow, System.StringComparer.Ordinal);
        return hash.ToHashCode();
    }

    public static bool operator ==(ScrollBarGlyphs a, ScrollBarGlyphs b) => a.Equals(b);

    public static bool operator !=(ScrollBarGlyphs a, ScrollBarGlyphs b) => !a.Equals(b);
    #endregion
}
