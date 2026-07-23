using CColor = ConsoleGUI.Data.Color;

namespace Jumbee.Console.Drawing;
/// <summary>
/// Converts canvas (world) coordinates to grid dot coordinates and paints them, for use by <see cref="IShape"/>
/// implementations. The origin of the canvas coordinate system is the bottom-left corner (unlike the terminal's
/// top-left), so <see cref="GetPoint"/> flips the y axis.
/// </summary>
internal sealed class Painter(CanvasContext context)
{

    /// <summary>The canvas coordinate bounds — (x: [left, right], y: [bottom, top]).</summary>
    public ((double Min, double Max) X, (double Min, double Max) Y) Bounds => (_context.XBounds, _context.YBounds);

    /// <summary>
    /// Maps canvas coordinates to grid dot coordinates, or <see langword="null"/> when the point is outside the
    /// bounds or the bounds are degenerate. Points round to the nearest dot, ties rounding away from zero (matching
    /// ratatui's <c>f64::round</c>).
    /// </summary>
    public (int X, int Y)? GetPoint(double x, double y)
    {
        var (left, right) = _context.XBounds;
        var (bottom, top) = _context.YBounds;
        if (x < left || x > right || y < bottom || y > top) return null;
        var width = right - left;
        var height = top - bottom;
        if (width <= 0.0 || height <= 0.0) return null;
        var gx = (int)Math.Round((x - left) * (_resolution.X - 1.0) / width, MidpointRounding.AwayFromZero);
        var gy = (int)Math.Round((top - y) * (_resolution.Y - 1.0) / height, MidpointRounding.AwayFromZero);
        return (gx, gy);
    }

    /// <summary>Paints the grid dot at (<paramref name="x"/>, <paramref name="y"/>) — grid coordinates from <see cref="GetPoint"/>.</summary>
    public void Paint(int x, int y, CColor color) => _context.Grid.Paint(x, y, color);

    private readonly CanvasContext _context = context;
    private readonly (double X, double Y) _resolution = context.Grid.Resolution;
}

/// <summary>
/// Holds the drawing state for one render of a <see cref="Jumbee.Console.Canvas"/>: the target grid, its coordinate
/// bounds, and the layers built so far. Shapes are drawn with <see cref="Draw"/>; <see cref="Layer"/> snapshots the
/// current grid and starts a fresh one; <see cref="Finish"/> flushes the final layer.
/// </summary>
internal sealed class CanvasContext(int width, int height, (double Min, double Max) xBounds, (double Min, double Max) yBounds, CanvasMarker marker, char customMarker)
{
    public (double Min, double Max) XBounds { get; } = xBounds;
    public (double Min, double Max) YBounds { get; } = yBounds;
    public IGrid Grid { get; private set; } = MarkerToGrid(width, height, marker, customMarker);
    public IReadOnlyList<Layer> Layers => _layers;

    /// <summary>Draws a shape onto the current grid.</summary>
    public void Draw(IShape shape)
    {
        _dirty = true;
        shape.Draw(new Painter(this));
    }

    /// <summary>Snapshots the current grid as a layer and resets it for the next layer.</summary>
    public void Layer()
    {
        _layers.Add(Grid.Save());
        Grid.Reset();
        _dirty = false;
    }

    /// <summary>Switches the marker for subsequent draws: flushes the current layer (if anything was drawn) and
    /// replaces the grid with one for <paramref name="marker"/>. Mirrors ratatui's <c>Context::marker</c>.</summary>
    public void Marker(CanvasMarker marker, char customMarker)
    {
        Finish();
        Grid = MarkerToGrid(_width, _height, marker, customMarker);
    }

    /// <summary>Flushes the current grid as a final layer if anything has been drawn since the last <see cref="Layer"/>.</summary>
    public void Finish()
    {
        if (_dirty) Layer();
    }

    private static IGrid MarkerToGrid(int width, int height, CanvasMarker marker, char customMarker) => marker switch
    {
        CanvasMarker.Block => new CharGrid(width, height, CanvasSymbols.Block, applyColorToBg: true),
        CanvasMarker.Bar => new CharGrid(width, height, CanvasSymbols.Bar),
        CanvasMarker.Braille => new PatternGrid(width, height, 2, 4, CanvasSymbols.Braille),
        CanvasMarker.HalfBlock => new HalfBlockGrid(width, height),
        CanvasMarker.Quadrant => new PatternGrid(width, height, 2, 2, CanvasSymbols.Quadrants),
        CanvasMarker.Custom => new CharGrid(width, height, customMarker),
        _ => new CharGrid(width, height, CanvasSymbols.Dot),
    };

    private readonly int _width = width;
    private readonly int _height = height;
    private readonly List<Layer> _layers = [];
    private bool _dirty;
}