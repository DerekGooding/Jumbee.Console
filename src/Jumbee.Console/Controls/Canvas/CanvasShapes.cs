using CColor = ConsoleGUI.Data.Color;

namespace Jumbee.Console.Drawing;

/// <summary>
/// A shape that can be drawn on a <see cref="Jumbee.Console.Canvas"/> via <see cref="Jumbee.Console.Canvas.Add"/>.
/// </summary>
/// <remarks>
/// The built-in shapes (<see cref="Line"/>, <see cref="FilledLine"/>, <see cref="Rectangle"/>, <see cref="Points"/>,
/// <see cref="Circle"/>) implement it; drawing goes through an internal painter, so this is a closed set.
/// </remarks>
public interface IShape
{
    /// <summary>Draws this shape using the given painter (internal drawing surface).</summary>
    internal void Draw(Painter painter);
}

/// <summary>A straight line between two points in canvas coordinates.</summary>
/// <remarks>Initializes a new <see cref="Line"/> from (<paramref name="x1"/>, <paramref name="y1"/>) to (<paramref name="x2"/>, <paramref name="y2"/>) in the given colour.</remarks>
public sealed class Line(double x1, double y1, double x2, double y2, Color color) : IShape
{
    /// <summary>X coordinate of the start point.</summary>
    public double X1 { get; } = x1;

    /// <summary>Y coordinate of the start point.</summary>
    public double Y1 { get; } = y1;

    /// <summary>X coordinate of the end point.</summary>
    public double X2 { get; } = x2;

    /// <summary>Y coordinate of the end point.</summary>
    public double Y2 { get; } = y2;

    /// <summary>Colour of the line.</summary>
    public Color Color { get; } = color;

    void IShape.Draw(Painter painter)
    {
        var (xb, yb) = painter.Bounds;
        if (LineMath.ClipLine(xb.Min, xb.Max, yb.Min, yb.Max, X1, Y1, X2, Y2) is not (double wx1, double wy1, double wx2, double wy2)) return;
        if (painter.GetPoint(wx1, wy1) is not (int gx1, int gy1)) return;
        if (painter.GetPoint(wx2, wy2) is not (int gx2, int gy2)) return;
        CColor color = Color;
        LineMath.ForEachLinePoint(gx1, gy1, gx2, gy2, (x, y) => painter.Paint(x, y, color));
    }
}

/// <summary>
/// A line whose area between the line and <see cref="FillToY"/> is filled — useful for area charts.
/// </summary>
/// <remarks>The fill runs vertically from each line point to the row of <see cref="FillToY"/>.</remarks>
/// <remarks>Initializes a new <see cref="FilledLine"/> from (<paramref name="x1"/>, <paramref name="y1"/>) to (<paramref name="x2"/>, <paramref name="y2"/>), filled down to <paramref name="fillToY"/>, in the given colour.</remarks>
public sealed class FilledLine(double x1, double y1, double x2, double y2, double fillToY, Color color) : IShape
{
    /// <summary>X coordinate of the start point.</summary>
    public double X1 { get; } = x1;

    /// <summary>Y coordinate of the start point.</summary>
    public double Y1 { get; } = y1;

    /// <summary>X coordinate of the end point.</summary>
    public double X2 { get; } = x2;

    /// <summary>Y coordinate of the end point.</summary>
    public double Y2 { get; } = y2;

    /// <summary>Y coordinate the fill extends to from each line point.</summary>
    public double FillToY { get; } = fillToY;

    /// <summary>Colour of the line and its fill.</summary>
    public Color Color { get; } = color;

    void IShape.Draw(Painter painter)
    {
        var (xb, yb) = painter.Bounds;
        if (LineMath.ClipLine(xb.Min, xb.Max, yb.Min, yb.Max, X1, Y1, X2, Y2) is not (double wx1, double wy1, double wx2, double wy2)) return;
        if (painter.GetPoint(wx1, wy1) is not (int gx1, int gy1)) return;
        if (painter.GetPoint(wx2, wy2) is not (int gx2, int gy2)) return;

        var clampedFill = Math.Clamp(FillToY, yb.Min, yb.Max);
        if (painter.GetPoint(wx1, clampedFill) is not (int _, int yFill)) return;

        CColor color = Color;
        LineMath.ForEachLinePoint(gx1, gy1, gx2, gy2, (x, y) =>
        {
            var start = Math.Min(y, yFill);
            var end = Math.Max(y, yFill);
            for (var fy = start; fy <= end; fy++)
                painter.Paint(x, fy, color);
        });
    }
}

/// <summary>
/// A rectangle outline. Positioned from its bottom-left corner (<see cref="X"/>, <see cref="Y"/>) in canvas
/// coordinates — the mathematical convention, not terminal cells.
/// </summary>
/// <remarks>Initializes a new <see cref="Rectangle"/> at bottom-left corner (<paramref name="x"/>, <paramref name="y"/>) with the given size and colour.</remarks>
public sealed class Rectangle(double x, double y, double width, double height, Color color) : IShape
{
    /// <summary>X coordinate of the bottom-left corner.</summary>
    public double X { get; } = x;

    /// <summary>Y coordinate of the bottom-left corner.</summary>
    public double Y { get; } = y;

    /// <summary>Width of the rectangle.</summary>
    public double Width { get; } = width;

    /// <summary>Height of the rectangle.</summary>
    public double Height { get; } = height;

    /// <summary>Colour of the outline.</summary>
    public Color Color { get; } = color;

    void IShape.Draw(Painter painter)
    {
        IShape left = new Line(X, Y, X, Y + Height, Color);
        IShape top = new Line(X, Y + Height, X + Width, Y + Height, Color);
        IShape right = new Line(X + Width, Y, X + Width, Y + Height, Color);
        IShape bottom = new Line(X, Y, X + Width, Y, Color);
        left.Draw(painter);
        top.Draw(painter);
        right.Draw(painter);
        bottom.Draw(painter);
    }
}

/// <summary>A scatter of individual points in canvas coordinates.</summary>
/// <remarks>Initializes a new <see cref="Points"/> scatter from the given coordinates and colour.</remarks>
public sealed class Points(IReadOnlyList<(double X, double Y)> coords, Color color) : IShape
{
    /// <summary>The point coordinates in canvas space.</summary>
    public IReadOnlyList<(double X, double Y)> Coords { get; } = coords;

    /// <summary>Colour of the points.</summary>
    public Color Color { get; } = color;

    void IShape.Draw(Painter painter)
    {
        CColor color = Color;
        foreach (var (x, y) in Coords)
            if (painter.GetPoint(x, y) is (int gx, int gy))
                painter.Paint(gx, gy, color);
    }
}

/// <summary>A circle outline traced at one-degree steps around its centre, in canvas coordinates.</summary>
/// <remarks>Initializes a new <see cref="Circle"/> centred at (<paramref name="x"/>, <paramref name="y"/>) with the given radius and colour.</remarks>
public sealed class Circle(double x, double y, double radius, Color color) : IShape
{
    /// <summary>X coordinate of the centre.</summary>
    public double X { get; } = x;

    /// <summary>Y coordinate of the centre.</summary>
    public double Y { get; } = y;

    /// <summary>Radius of the circle.</summary>
    public double Radius { get; } = radius;

    /// <summary>Colour of the outline.</summary>
    public Color Color { get; } = color;

    void IShape.Draw(Painter painter)
    {
        CColor color = Color;
        for (var angle = 0; angle < 360; angle++)
        {
            var radians = angle * Math.PI / 180.0;
            var cx = (Radius * Math.Cos(radians)) + X;
            var cy = (Radius * Math.Sin(radians)) + Y;
            if (painter.GetPoint(cx, cy) is (int gx, int gy))
                painter.Paint(gx, gy, color);
        }
    }
}

/// <summary>Line rasterisation (Bresenham) and clipping (Cohen–Sutherland) shared by the line shapes.</summary>
internal static class LineMath
{
    /// <summary>Invokes <paramref name="plot"/> for each grid dot on the Bresenham line from (x1,y1) to (x2,y2).</summary>
    public static void ForEachLinePoint(int x1, int y1, int x2, int y2, Action<int, int> plot)
    {
        var dx = Math.Abs(x2 - x1);
        var dy = Math.Abs(y2 - y1);

        if (dx == 0)
        {
            for (var y = Math.Min(y1, y2); y <= Math.Max(y1, y2); y++) plot(x1, y);
        }
        else if (dy == 0)
        {
            for (var x = Math.Min(x1, x2); x <= Math.Max(x1, x2); x++) plot(x, y1);
        }
        else if (dy < dx)
        {
            if (x1 > x2) LowLine(x2, y2, x1, y1, plot);
            else LowLine(x1, y1, x2, y2, plot);
        }
        else if (y1 > y2)
        {
            HighLine(x2, y2, x1, y1, plot);
        }
        else
        {
            HighLine(x1, y1, x2, y2, plot);
        }
    }

    // Shallow slope (|dy| < dx), stepping x by one and y as the error accumulates. x1 <= x2 guaranteed by the caller.
    private static void LowLine(int x1, int y1, int x2, int y2, Action<int, int> plot)
    {
        var dx = x2 - x1;
        var dy = Math.Abs(y2 - y1);
        var d = (2 * dy) - dx;
        var y = y1;
        var descending = y1 > y2;
        for (var x = x1; x <= x2; x++)
        {
            plot(x, y);
            if (d > 0)
            {
                y = descending ? Math.Max(0, y - 1) : y + 1;
                d -= 2 * dx;
            }
            d += 2 * dy;
        }
    }

    // Steep slope (|dy| >= dx), stepping y by one and x as the error accumulates. y1 <= y2 guaranteed by the caller.
    private static void HighLine(int x1, int y1, int x2, int y2, Action<int, int> plot)
    {
        var dx = Math.Abs(x2 - x1);
        var dy = y2 - y1;
        var d = (2 * dx) - dy;
        var x = x1;
        var leftward = x1 > x2;
        for (var y = y1; y <= y2; y++)
        {
            plot(x, y);
            if (d > 0)
            {
                x = leftward ? Math.Max(0, x - 1) : x + 1;
                d -= 2 * dy;
            }
            d += 2 * dx;
        }
    }

    /// <summary>
    /// Clips the segment (x1,y1)-(x2,y2) to the rectangle [xmin,xmax]×[ymin,ymax] (Cohen–Sutherland), returning the
    /// clipped endpoints or <see langword="null"/> when the segment lies wholly outside.
    /// </summary>
    public static (double X1, double Y1, double X2, double Y2)? ClipLine(
        double xmin, double xmax, double ymin, double ymax, double x1, double y1, double x2, double y2)
    {
        var code1 = RegionCode(x1, y1, xmin, xmax, ymin, ymax);
        var code2 = RegionCode(x2, y2, xmin, xmax, ymin, ymax);

        while (true)
        {
            if ((code1 | code2) == 0) return (x1, y1, x2, y2);   // both endpoints inside
            if ((code1 & code2) != 0) return null;               // both share an outside region -> fully outside

            var code = code1 != 0 ? code1 : code2;
            double x, y;
            if ((code & Top) != 0)
            {
                x = x1 + ((x2 - x1) * (ymax - y1) / (y2 - y1));
                y = ymax;
            }
            else if ((code & Bottom) != 0)
            {
                x = x1 + ((x2 - x1) * (ymin - y1) / (y2 - y1));
                y = ymin;
            }
            else if ((code & Right) != 0)
            {
                y = y1 + ((y2 - y1) * (xmax - x1) / (x2 - x1));
                x = xmax;
            }
            else // Left
            {
                y = y1 + ((y2 - y1) * (xmin - x1) / (x2 - x1));
                x = xmin;
            }

            if (code == code1)
            {
                x1 = x; y1 = y;
                code1 = RegionCode(x1, y1, xmin, xmax, ymin, ymax);
            }
            else
            {
                x2 = x; y2 = y;
                code2 = RegionCode(x2, y2, xmin, xmax, ymin, ymax);
            }
        }
    }

    private const int Left = 1, Right = 2, Bottom = 4, Top = 8;

    private static int RegionCode(double x, double y, double xmin, double xmax, double ymin, double ymax)
    {
        var code = 0;
        if (x < xmin) code |= Left;
        else if (x > xmax) code |= Right;
        if (y < ymin) code |= Bottom;
        else if (y > ymax) code |= Top;
        return code;
    }
}