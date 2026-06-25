namespace Jumbee.Console;

/// <summary>
/// The glyphs (no colours) a control frame's vertical scrollbar draws for each part: the moving thumb, the track
/// behind it, and the two end arrows. Colours/decorations come separately from <see cref="ScrollBarStyle"/> (via
/// <see cref="IStyleTheme.ScrollBar"/>); a control frame composes the two into its scrollbar cells. The static
/// presets are convenience helpers for restyling a single control's glyphs (e.g. via WithScrollBarGlyphs).
/// </summary>
public readonly struct ScrollBarGlyphs
{
    #region Constructors
    public ScrollBarGlyphs(string thumb, string track, string upArrow, string downArrow)
    {
        Thumb = thumb;
        Track = track;
        UpArrow = upArrow;
        DownArrow = downArrow;
    }
    #endregion

    #region Properties
    /// <summary>The glyph for the part of the track currently in view (the draggable handle).</summary>
    public string Thumb { get; init; }

    /// <summary>The glyph for the track behind the thumb.</summary>
    public string Track { get; init; }

    /// <summary>The glyph at the top end of the scrollbar.</summary>
    public string UpArrow { get; init; }

    /// <summary>The glyph at the bottom end of the scrollbar.</summary>
    public string DownArrow { get; init; }
    #endregion

    #region Presets
    /// <summary>The original default glyphs: a '#' thumb on a '|' track with triangle arrows.</summary>
    public static ScrollBarGlyphs Default { get; } = new("#", "|", "▲", "▼");

    /// <summary>A solid block thumb on a light vertical-line track with triangle arrows.</summary>
    public static ScrollBarGlyphs Block { get; } = new("█", "│", "▲", "▼");

    /// <summary>A shaded (dithered) thumb on a light vertical-line track with thin line arrows.</summary>
    public static ScrollBarGlyphs Shaded { get; } = new("▒", "│", "↑", "↓");

    /// <summary>A heavy vertical-line thumb on a light vertical-line track with triangle arrows.</summary>
    public static ScrollBarGlyphs Line { get; } = new("┃", "│", "▲", "▼");
    #endregion
}
