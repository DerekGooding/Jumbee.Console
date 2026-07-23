
using ConsoleGUI.Space;
using CColor = ConsoleGUI.Data.Color;

namespace Jumbee.Console.Documents;
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

    #endregion Constructors

    #region Properties

    public int Width { get; }
    public int Height { get; }

    #endregion Properties

    #region Methods

    // Direction bits.
    private const int U = 1, D = 2, L = 4, R = 8;

    /// <summary>Line weight for <see cref="Link"/>: normal, heavy (thick edges), or dashed (dotted edges).</summary>
    public enum LineStyle : byte
    { Normal = 0, Heavy = 1, Dashed = 2 }

    /// <summary>Border style for <see cref="Box"/>: square corners, rounded (arc) corners, double lines, or heavy lines.
    /// Used to distinguish node kinds (e.g. decision vs process) without drawing non-rectangular shapes.</summary>
    public enum BoxBorder : byte
    { Square = 0, Rounded = 1, Double = 2, Heavy = 3 }

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
        if (dx == 1) { AddBit(a.x, a.y, R, color, style); AddBit(b.x, b.y, L, color, style); }
        else if (dx == -1) { AddBit(a.x, a.y, L, color, style); AddBit(b.x, b.y, R, color, style); }
        else if (dy == 1) { AddBit(a.x, a.y, D, color, style); AddBit(b.x, b.y, U, color, style); }
        else if (dy == -1) { AddBit(a.x, a.y, U, color, style); AddBit(b.x, b.y, D, color, style); }
    }

    /// <summary>Draws a rectangle outline as line cells (mask-merged), so crossing edges resolve to junctions.
    /// Used for subgraph groups, which edges routinely pass through.</summary>
    public void LinkRect(int x0, int y0, int x1, int y1, CColor? color)
    {
        for (var x = x0; x < x1; x++) { Link((x, y0), (x + 1, y0), color); Link((x, y1), (x + 1, y1), color); }
        for (var y = y0; y < y1; y++) { Link((x0, y), (x0, y + 1), color); Link((x1, y), (x1, y + 1), color); }
    }

    /// <summary>Draws a box outline (square corners) and clears its interior.</summary>
    public void Box(int x0, int y0, int x1, int y1, bool rounded, CColor? color) =>
        Box(x0, y0, x1, y1, rounded ? BoxBorder.Rounded : BoxBorder.Square, color);

    /// <summary>Draws a box outline in the given <paramref name="border"/> style and clears its interior.</summary>
    public void Box(int x0, int y0, int x1, int y1, BoxBorder border, CColor? color)
    {
        for (var y = y0 + 1; y < y1; y++)
            for (var x = x0 + 1; x < x1; x++)
                Clear(x, y);

        // (horizontal, vertical, top-left, top-right, bottom-left, bottom-right) glyphs per border style.
        var (h, v, tl, tr, bl, br) = border switch
        {
            BoxBorder.Rounded => ('─', '│', '╭', '╮', '╰', '╯'),
            BoxBorder.Double => ('═', '║', '╔', '╗', '╚', '╝'),
            BoxBorder.Heavy => ('━', '┃', '┏', '┓', '┗', '┛'),
            _ => ('─', '│', '┌', '┐', '└', '┘'),
        };

        for (var x = x0 + 1; x < x1; x++) { SetChar(x, y0, h, color); SetChar(x, y1, h, color); }
        for (var y = y0 + 1; y < y1; y++) { SetChar(x0, y, v, color); SetChar(x1, y, v, color); }
        SetChar(x0, y0, tl, color);
        SetChar(x1, y0, tr, color);
        SetChar(x0, y1, bl, color);
        SetChar(x1, y1, br, color);
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

    /// <summary>Orthogonally-connected rasterization between two cells: every consecutive cell differs by exactly one
    /// axis, so a diagonal segment becomes a connected staircase (proper corners) rather than gaps. Shared by the
    /// flowchart and class renderers for edge polylines.</summary>
    public static IEnumerable<(int x, int y)> OrthoLine(int x0, int y0, int x1, int y1)
    {
        int x = x0, y = y0;
        int dx = Math.Abs(x1 - x0), dy = Math.Abs(y1 - y0);
        int sx = x0 <= x1 ? 1 : -1, sy = y0 <= y1 ? 1 : -1;
        int px = 0, py = 0;
        yield return (x, y);
        while (x != x1 || y != y1)
        {
            if (y == y1) x += sx;
            else if (x == x1) y += sy;
            else if ((2 * px + 1) * dy <= (2 * py + 1) * dx) { x += sx; px++; }
            else { y += sy; py++; }
            yield return (x, y);
        }
    }

    /// <summary>The glyph at (x, y); a space when out of range.</summary>
    internal char GlyphAt(int x, int y) => (uint)x < (uint)Width && (uint)y < (uint)Height ? _chars[y * Width + x] : ' ';

    /// <summary>The foreground colour at (x, y), or <see langword="null"/> when unset or out of range.</summary>
    internal CColor? ColorAt(int x, int y) => (uint)x < (uint)Width && (uint)y < (uint)Height ? _fg[y * Width + x] : null;

    /// <summary>Wraps this canvas as a Spectre.Console renderable so a rasterized diagram can compose into a
    /// renderable tree (e.g. an embedded <c>[source,mermaid]</c> block in the AsciiDoc viewer).</summary>
    public Spectre.Console.Rendering.IRenderable ToRenderable() => new CellCanvasRenderable(this);

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
            U | D => '┃',
            L | R => '━',
            D | R => '┏',
            D | L => '┓',
            U | R => '┗',
            U | L => '┛',
            U | D | R => '┣',
            U | D | L => '┫',
            D | L | R => '┳',
            U | L | R => '┻',
            U | D | L | R => '╋',
            U or D => '┃',
            L or R => '━',
            _ => ' ',
        },
        LineStyle.Dashed => mask switch   // dashed straights, ordinary junctions (no dashed corner glyphs exist)
        {
            U | D => '┆',
            L | R => '┄',
            U or D => '┆',
            L or R => '┄',
            _ => LightGlyph(mask),
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

    #endregion Methods

    #region Fields

    private readonly char[] _chars;
    private readonly CColor?[] _fg;
    private readonly byte[] _mask;
    private readonly byte[] _lineStyle;

    #endregion Fields
}