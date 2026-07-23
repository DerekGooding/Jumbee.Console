
using CColor = ConsoleGUI.Data.Color;

namespace Jumbee.Console.Drawing;
/// <summary>
/// A single painted cell of a <see cref="Layer"/>. The three properties are independent: when layers are composited,
/// each is taken from the top-most layer that supplies it, so (for example) a <see cref="CanvasMarker.Block"/> layer
/// can set a background while a later braille layer sets the symbol and foreground on the same cell.
/// </summary>
internal readonly struct LayerCell(char? symbol, CColor? fg, CColor? bg)
{
    public readonly char? Symbol = symbol;
    public readonly CColor? Fg = fg;
    public readonly CColor? Bg = bg;
}

/// <summary>A saved snapshot of a grid — one <see cref="LayerCell"/> per terminal cell, in row-major order.</summary>
internal sealed class Layer(LayerCell[] contents)
{
    public LayerCell[] Contents { get; } = contents;
}

/// <summary>
/// A grid of terminal cells that shapes are painted onto. The grid's dot <see cref="Resolution"/> may exceed its cell
/// dimensions (e.g. a braille grid resolves 2×4 dots per cell). Painting is expressed in dot coordinates with the
/// origin at the top-left; <see cref="Save"/> collapses the current dots into a renderable <see cref="Layer"/>.
/// </summary>
internal interface IGrid
{
    /// <summary>The grid resolution in dots — not necessarily the cell dimensions (braille = 2×4 dots per cell).</summary>
    (double X, double Y) Resolution { get; }

    /// <summary>Paints the dot at (<paramref name="x"/>, <paramref name="y"/>). Out-of-range dots are ignored, never thrown.</summary>
    void Paint(int x, int y, CColor color);

    /// <summary>Snapshots the current dots as a layer to be composited.</summary>
    Layer Save();

    /// <summary>Clears all dots back to the initial (blank) state.</summary>
    void Reset();
}

/// <summary>
/// A grid whose cells each hold a <c>W</c>×<c>H</c> pattern glyph (braille 2×4, quadrant 2×2), giving sub-cell
/// resolution with a single foreground colour per cell. The pattern is accumulated as a row-major bit mask
/// (bit = <c>col + W·row</c>) and mapped to a glyph via the lookup table on save.
/// </summary>
internal sealed class PatternGrid(int width, int height, int cellWidth, int cellHeight, char[] charTable) : IGrid
{
    public (double X, double Y) Resolution => (_width * (double)_cellWidth, _height * (double)_cellHeight);

    public void Paint(int x, int y, CColor color)
    {
        if (x < 0 || y < 0) return;
        var index = ((y / _cellHeight) * _width) + (x / _cellWidth);
        if ((uint)index >= (uint)_patterns.Length) return;
        _patterns[index] |= (byte)(1 << ((x % _cellWidth) + (_cellWidth * (y % _cellHeight))));
        _colors[index] = color;
    }

    public Layer Save()
    {
        var contents = new LayerCell[_patterns.Length];
        for (var i = 0; i < _patterns.Length; i++)
        {
            var pattern = _patterns[i];
            // A blank pattern leaves the cell untouched so lower layers show through; patterns only set a foreground.
            char? symbol = pattern == 0 ? null : _charTable[pattern];
            contents[i] = new LayerCell(symbol, _colors[i], null);
        }
        return new Layer(contents);
    }

    public void Reset()
    {
        System.Array.Clear(_patterns);
        System.Array.Clear(_colors);
    }

    private readonly int _width = width;
    private readonly int _height = height;
    private readonly int _cellWidth = cellWidth;
    private readonly int _cellHeight = cellHeight;
    private readonly char[] _charTable = charTable;
    private readonly byte[] _patterns = new byte[width * height];
    private readonly CColor?[] _colors = new CColor?[width * height];
}

/// <summary>
/// A grid whose cells each hold a single fixed character (dot, block, bar, or a custom glyph), giving 1×1 resolution.
/// When <paramref name="applyColorToBg"/> is set (used by <see cref="CanvasMarker.Block"/>) the cell's colour is also
/// written as the background, so it overwrites any earlier glyph yet still lets a later layer overlay a foreground.
/// </summary>
internal sealed class CharGrid(int width, int height, char cellChar, bool applyColorToBg = false) : IGrid
{
    public (double X, double Y) Resolution => (_width, _height);

    public void Paint(int x, int y, CColor color)
    {
        if (x < 0 || y < 0) return;
        var index = (y * _width) + x;
        if ((uint)index >= (uint)_colors.Length) return;
        _colors[index] = color;
    }

    public Layer Save()
    {
        var contents = new LayerCell[_colors.Length];
        for (var i = 0; i < _colors.Length; i++)
        {
            var color = _colors[i];
            char? symbol = color.HasValue ? _cellChar : null;
            var bg = color.HasValue && _applyColorToBg ? color : null;
            contents[i] = new LayerCell(symbol, color, bg);
        }
        return new Layer(contents);
    }

    public void Reset() => System.Array.Clear(_colors);

    private readonly int _width = width;
    private readonly int _height = height;
    private readonly char _cellChar = cellChar;
    private readonly bool _applyColorToBg = applyColorToBg;
    private readonly CColor?[] _colors = new CColor?[width * height];
}

/// <summary>
/// A grid of vertical half-block cells — 1×2 dots per cell — that gives each cell an independent colour for its upper
/// and lower dot. On save each vertical pair of dots collapses to a space, <c>▀</c>, <c>▄</c> or <c>█</c> with the
/// foreground/background chosen to reproduce the two dot colours.
/// </summary>
internal sealed class HalfBlockGrid(int width, int height) : IGrid
{
    public (double X, double Y) Resolution => (_width, _height * 2.0);

    public void Paint(int x, int y, CColor color)
    {
        if (x < 0 || y < 0 || x >= _width || y >= _height * 2) return;
        _pixels[(y * _width) + x] = color;
    }

    public Layer Save()
    {
        var contents = new LayerCell[_width * _height];
        for (var row = 0; row < _height; row++)
        {
            var upperBase = row * 2 * _width;
            var lowerBase = ((row * 2) + 1) * _width;
            for (var x = 0; x < _width; x++)
            {
                var upper = _pixels[upperBase + x];
                var lower = _pixels[lowerBase + x];
                // 1. neither set     -> blank, show lower layers through
                // 2. only lower set  -> '▄' fg=lower
                // 3. only upper set  -> '▀' fg=upper
                // 4. both, same      -> '█' fg=upper bg=lower (a solid cell; keeps unit tests single-char)
                // 5. both, different -> '▀' fg=upper bg=lower
                LayerCell cell;
                if (!upper.HasValue && !lower.HasValue)
                {
                    cell = new LayerCell(null, null, null);
                }
                else if (!upper.HasValue)
                {
                    cell = new LayerCell(CanvasSymbols.HalfLower, lower, null);
                }
                else if (!lower.HasValue)
                {
                    cell = new LayerCell(CanvasSymbols.HalfUpper, upper, null);
                }
                else
                {
                    cell = upper.Value == lower.Value
                                    ? new LayerCell(CanvasSymbols.HalfFull, upper, lower)
                                    : new LayerCell(CanvasSymbols.HalfUpper, upper, lower);
                }

                contents[row * _width + x] = cell;
            }
        }
        return new Layer(contents);
    }

    public void Reset() => System.Array.Clear(_pixels);

    private readonly int _width = width;
    private readonly int _height = height;
    private readonly CColor?[] _pixels = new CColor?[width * height * 2];
}