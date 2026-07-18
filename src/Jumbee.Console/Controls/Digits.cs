namespace Jumbee.Console;

using System;
using System.Collections.Generic;

using Spectre.Console.Rendering;

/// <summary>
/// Renders text using large three-row "seven-segment" glyphs, for clocks, counters and headline figures.
/// </summary>
/// <remarks>
/// Supports the digits <c>0-9</c> and the symbols <c>. , : - + space</c>; unsupported characters render blank.
/// Three rows tall and three cells wide per character.
/// </remarks>
public class Digits : RenderableControl
{
    #region Constructors
    /// <summary>Initializes a new <see cref="Digits"/> displaying the given text in large glyphs.</summary>
    public Digits(string text = "")
    {
        Focusable = false;   // a passive display control: never a focus/tab target
        _text = text ?? "";
        Height = GlyphRows;
        Width = _text.Length * GlyphWidth;
        ApplyTheme();
    }
    #endregion

    #region Properties
    /// <summary>The text to render in large glyphs. Setting it re-sizes the control.</summary>
    public string Text
    {
        get => _text;
        set => SetAtomicProperty(ref _text, value ?? "", updatesLayout: true, watch: (_, v) => Width = v.Length * GlyphWidth);
    }

    /// <summary>The glyph style. Defaults to <see cref="IStyleTheme.TextAccent"/>.</summary>
    public Style DigitStyle { get => _digitStyle; set => SetAtomicProperty(ref _digitStyle, value, themeOverride: true); }
    #endregion

    #region Methods
    /// <inheritdoc/>
    protected override void ApplyTheme()
    {
        if (!IsThemeOverridden(nameof(DigitStyle))) _digitStyle = UI.StyleTheme.TextAccent;
    }

    // Content-only render (never reads focus/hover): reuse the cached buffer on interactive-state changes.
    /// <inheritdoc/>
    protected override bool RendersInteractiveState => false;

    /// <inheritdoc/>
    protected override Measurement Measure(RenderOptions options, int maxWidth)
    {
        var width = Math.Min(_text.Length * GlyphWidth, maxWidth);
        return new Measurement(width, width);
    }

    /// <inheritdoc/>
    protected override IEnumerable<Segment> Render(RenderOptions options, int maxWidth)
    {
        var style = _digitStyle.SpectreConsoleStyle;
        for (var row = 0; row < GlyphRows; row++)
        {
            var sb = new System.Text.StringBuilder(_text.Length * GlyphWidth);
            foreach (var c in _text)
            {
                sb.Append(Font.TryGetValue(c, out var rows) ? rows[row] : Blank);
            }
            yield return new Segment(sb.ToString(), style);
            if (row < GlyphRows - 1) yield return Segment.LineBreak;
        }
    }
    #endregion

    #region Fields
    private const int GlyphRows = 3;
    private const int GlyphWidth = 3;
    private const string Blank = "   ";

    private string _text;
    private Style _digitStyle;

    // Three-row seven-segment glyphs (each row is three cells wide).
    private static readonly Dictionary<char, string[]> Font = new()
    {
        ['0'] = [" _ ", "| |", "|_|"],
        ['1'] = ["   ", "  |", "  |"],
        ['2'] = [" _ ", " _|", "|_ "],
        ['3'] = [" _ ", " _|", " _|"],
        ['4'] = ["   ", "|_|", "  |"],
        ['5'] = [" _ ", "|_ ", " _|"],
        ['6'] = [" _ ", "|_ ", "|_|"],
        ['7'] = [" _ ", "  |", "  |"],
        ['8'] = [" _ ", "|_|", "|_|"],
        ['9'] = [" _ ", "|_|", " _|"],
        [' '] = ["   ", "   ", "   "],
        [':'] = ["   ", " . ", " . "],
        ['.'] = ["   ", "   ", " . "],
        [','] = ["   ", "   ", " , "],
        ['-'] = ["   ", " _ ", "   "],
        ['+'] = ["   ", "|_|", " | "],
    };
    #endregion
}
