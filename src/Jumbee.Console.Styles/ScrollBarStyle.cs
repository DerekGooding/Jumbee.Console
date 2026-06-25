namespace Jumbee.Console;

using ConsoleGUI.Data;

/// <summary>
/// Describes the glyphs and colors used to draw a control frame's vertical scrollbar: the up/down end arrows,
/// the moving thumb, and the track behind it. Each part is a <see cref="Character"/> carrying its own glyph and
/// (optional) foreground/background colors. The static presets (<see cref="Default"/>, <see cref="Block"/>,
/// <see cref="Shaded"/>, <see cref="Line"/>) are convenience helpers for quickly restyling a single control;
/// a frame's default scrollbar comes from the active <see cref="IGlyphTheme.ScrollBar"/>.
/// </summary>
public readonly struct ScrollBarStyle
{
    #region Constructors
    public ScrollBarStyle(Character thumb, Character track, Character upArrow, Character downArrow)
    {
        Thumb = thumb;
        Track = track;
        UpArrow = upArrow;
        DownArrow = downArrow;
    }
    #endregion

    #region Properties
    /// <summary>The glyph drawn for the part of the track currently in view (the draggable handle).</summary>
    public Character Thumb { get; init; }

    /// <summary>The glyph drawn for the track behind the thumb.</summary>
    public Character Track { get; init; }

    /// <summary>The glyph drawn at the top end of the scrollbar.</summary>
    public Character UpArrow { get; init; }

    /// <summary>The glyph drawn at the bottom end of the scrollbar.</summary>
    public Character DownArrow { get; init; }
    #endregion

    #region Methods
    /// <summary>
    /// Returns a copy with the foreground colors overridden. A <c>null</c> argument leaves that
    /// part's existing color unchanged; <paramref name="arrows"/> recolors both end arrows.
    /// </summary>
    public ScrollBarStyle WithColors(Color? thumb = null, Color? track = null, Color? arrows = null) =>
        new ScrollBarStyle(
            thumb is { } t ? Thumb.WithForeground(t) : Thumb,
            track is { } r ? Track.WithForeground(r) : Track,
            arrows is { } a ? UpArrow.WithForeground(a) : UpArrow,
            arrows is { } d ? DownArrow.WithForeground(d) : DownArrow);
    #endregion

    #region Presets
    /// <summary>The original default style: a '#' thumb on a '|' track with triangle arrows.</summary>
    public static ScrollBarStyle Default { get; } = new(
        new Character('#', foreground: new Color(100, 100, 255)),
        new Character('|', foreground: new Color(100, 100, 100)),
        new Character('▲'),
        new Character('▼'));

    /// <summary>A solid block thumb on a light vertical-line track with triangle arrows.</summary>
    public static ScrollBarStyle Block { get; } = new(
        new Character('█'),
        new Character('│'),
        new Character('▲'),
        new Character('▼'));

    /// <summary>A shaded (dithered) thumb on a light vertical-line track with thin line arrows.</summary>
    public static ScrollBarStyle Shaded { get; } = new(
        new Character('▒'),
        new Character('│'),
        new Character('↑'),
        new Character('↓'));

    /// <summary>A heavy vertical-line thumb on a light vertical-line track with triangle arrows.</summary>
    public static ScrollBarStyle Line { get; } = new(
        new Character('┃'),
        new Character('│'),
        new Character('▲'),
        new Character('▼'));
    #endregion
}
