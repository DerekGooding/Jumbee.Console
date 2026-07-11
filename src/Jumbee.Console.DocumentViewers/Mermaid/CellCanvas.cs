namespace Jumbee.Console.DocumentViewers;

using System;

using ConsoleGUI.Space;

using CColor = ConsoleGUI.Data.Color;

/// <summary>
/// A fixed-size grid of styled characters with box-drawing primitives, used to rasterize a laid-out Mermaid diagram.
/// Line cells accumulate a direction bitmask (up/down/left/right) so crossings and bends resolve to the correct
/// junction glyph (<c>┼ ├ ┤ ┬ ┴ ┌ ┐ └ ┘</c>); node boxes and labels are written opaquely on top. Blits to a
/// <see cref="ConsoleBuffer"/>.
/// </summary>
internal sealed class CellCanvas
{
    #region Constructors
    public CellCanvas(int width, int height)
    {
        Width = Math.Max(1, width);
        Height = Math.Max(1, height);
        _chars = new char[Width * Height];
        _fg = new CColor?[Width * Height];
        _mask = new byte[Width * Height];
        _lineStyle = new byte[Width * Height];
        Array.Fill(_chars, ' ');
    }
    #endregion

    #region Properties
    public int Width { get; }
    public int Height { get; }
    #endregion

    #region Methods
    // Direction bits.
    private const int U = 1, D = 2, L = 4, R = 8;

    /// <summary>Line weight for <see cref="Link"/>: normal, heavy (thick edges), or dashed (dotted edges).</summary>
    public enum LineStyle : byte { Normal = 0, Heavy = 1, Dashed = 2 }

    /// <summary>Writes an opaque glyph (node borders, labels, arrowheads), clearing any accumulated line mask.</summary>
    public void SetChar(int x, int y, char c, CColor? color)
    {
        if ((uint)x >= (uint)Width || (uint)y >= (uint)Height) return;
        var i = y * Width + x;
        _chars[i] = c;
        _fg[i] = color;
        _mask[i] = 0;
    }

    /// <summary>Clears a cell to blank (used to fill node interiors so edges don't show through).</summary>
    public void Clear(int x, int y)
    {
        if ((uint)x >= (uint)Width || (uint)y >= (uint)Height) return;
        var i = y * Width + x;
        _chars[i] = ' ';
        _fg[i] = null;
        _mask[i] = 0;
    }

    /// <summary>Links two orthogonally-adjacent cells with a line segment, merging into junction glyphs.</summary>
    public void Link((int x, int y) a, (int x, int y) b, CColor? color, LineStyle style = LineStyle.Normal)
    {
        var dx = b.x - a.x;
        var dy = b.y - a.y;
        if (dx == 1)       { AddBit(a.x, a.y, R, color, style); AddBit(b.x, b.y, L, color, style); }
        else if (dx == -1) { AddBit(a.x, a.y, L, color, style); AddBit(b.x, b.y, R, color, style); }
        else if (dy == 1)  { AddBit(a.x, a.y, D, color, style); AddBit(b.x, b.y, U, color, style); }
        else if (dy == -1) { AddBit(a.x, a.y, U, color, style); AddBit(b.x, b.y, D, color, style); }
    }

    /// <summary>Draws a rectangle outline as line cells (mask-merged), so crossing edges resolve to junctions.
    /// Used for subgraph groups, which edges routinely pass through.</summary>
    public void LinkRect(int x0, int y0, int x1, int y1, CColor? color)
    {
        for (var x = x0; x < x1; x++) { Link((x, y0), (x + 1, y0), color); Link((x, y1), (x + 1, y1), color); }
        for (var y = y0; y < y1; y++) { Link((x0, y), (x0, y + 1), color); Link((x1, y), (x1, y + 1), color); }
    }

    /// <summary>Draws a box outline and clears its interior. <paramref name="rounded"/> picks arc corners.</summary>
    public void Box(int x0, int y0, int x1, int y1, bool rounded, CColor? color)
    {
        for (var y = y0 + 1; y < y1; y++)
            for (var x = x0 + 1; x < x1; x++)
                Clear(x, y);

        for (var x = x0 + 1; x < x1; x++) { SetChar(x, y0, '─', color); SetChar(x, y1, '─', color); }
        for (var y = y0 + 1; y < y1; y++) { SetChar(x0, y, '│', color); SetChar(x1, y, '│', color); }
        SetChar(x0, y0, rounded ? '╭' : '┌', color);
        SetChar(x1, y0, rounded ? '╮' : '┐', color);
        SetChar(x0, y1, rounded ? '╰' : '└', color);
        SetChar(x1, y1, rounded ? '╯' : '┘', color);
    }

    /// <summary>Writes text starting at (x,y), clipped to the right edge; does not wrap.</summary>
    public void Text(int x, int y, ReadOnlySpan<char> text, CColor? color)
    {
        foreach (var c in text)
        {
            if (c == '\n') return;
            SetChar(x++, y, c, color);
        }
    }

    public void Blit(ConsoleBuffer buffer)
    {
        var h = Math.Min(buffer.Size.Height, Height);
        var w = Math.Min(buffer.Size.Width, Width);
        for (var y = 0; y < h; y++)
            for (var x = 0; x < w; x++)
            {
                var i = y * Width + x;
                buffer.Write(new Position(x, y), new ConsoleGUI.Data.Character(_chars[i], _fg[i], null, null));
            }
    }

    private void AddBit(int x, int y, int bit, CColor? color, LineStyle style)
    {
        if ((uint)x >= (uint)Width || (uint)y >= (uint)Height) return;
        var i = y * Width + x;
        _mask[i] |= (byte)bit;
        if (style != LineStyle.Normal) _lineStyle[i] = (byte)style;   // a crossing keeps the more-emphatic style
        _chars[i] = GlyphFor(_mask[i], (LineStyle)_lineStyle[i]);
        if (color is not null) _fg[i] = color;
    }

    private static char GlyphFor(int mask, LineStyle style) => style switch
    {
        LineStyle.Heavy => mask switch
        {
            U | D => '┃', L | R => '━', D | R => '┏', D | L => '┓', U | R => '┗', U | L => '┛',
            U | D | R => '┣', U | D | L => '┫', D | L | R => '┳', U | L | R => '┻', U | D | L | R => '╋',
            U or D => '┃', L or R => '━', _ => ' ',
        },
        LineStyle.Dashed => mask switch   // dashed straights, ordinary junctions (no dashed corner glyphs exist)
        {
            U | D => '┆', L | R => '┄', U or D => '┆', L or R => '┄', _ => LightGlyph(mask),
        },
        _ => LightGlyph(mask),
    };

    private static char LightGlyph(int mask) => mask switch
    {
        U | D => '│',
        L | R => '─',
        D | R => '┌',
        D | L => '┐',
        U | R => '└',
        U | L => '┘',
        U | D | R => '├',
        U | D | L => '┤',
        D | L | R => '┬',
        U | L | R => '┴',
        U | D | L | R => '┼',
        U or D => '│',
        L or R => '─',
        _ => ' ',
    };
    #endregion

    #region Fields
    private readonly char[] _chars;
    private readonly CColor?[] _fg;
    private readonly byte[] _mask;
    private readonly byte[] _lineStyle;
    #endregion
}
