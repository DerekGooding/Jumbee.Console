namespace Jumbee.Console.Tests;

using System.Text;

using Jumbee.Console;
using Jumbee.Console.Drawing;
using Jumbee.Console.Snapshot;

using Xunit;

using CColor = ConsoleGUI.Data.Color;

/// <summary>
/// Ported golden vectors from Ratatui's canvas widget (canvas.rs, line.rs, rectangle.rs, circle.rs). The Sextant and
/// Octant markers are omitted (their supra-BMP glyphs cannot live in a single-char cell); every other marker's grid
/// is verified verbatim. Text snapshots compare glyphs only, so shape colours are arbitrary here.
/// </summary>
public class CanvasTests
{
    private static readonly CColor White = new(255, 255, 255);

    // Builds the expected snapshot the way ConsoleSnapshot.ToText emits one: rows joined by '\n' (trailing '\n' after
    // each), every row right-trimmed of spaces. Ratatui's per-marker fixtures fill untouched cells with 'x'; our
    // Initialize fills them with spaces, so 'x' maps to a space (then trims).
    private static string Expected(string rows)
    {
        var sb = new StringBuilder();
        foreach (var row in rows.Split('\n'))
            sb.Append(row.Replace('x', ' ').TrimEnd(' ')).Append('\n');
        return sb.ToString();
    }

    #region Painter.GetPoint doctest (canvas.rs Painter examples)
    [Fact]
    public void GetPoint_MapsCanvasCoordinatesToGridDots()
    {
        // ctx 2×2, x∈[1,2], y∈[0,2], Braille → resolution 4×8.
        var ctx = new CanvasContext(2, 2, (1.0, 2.0), (0.0, 2.0), CanvasMarker.Braille, ' ');
        var p = new Painter(ctx);

        Assert.Equal((0, 7), p.GetPoint(1.0, 0.0));
        Assert.Equal((2, 4), p.GetPoint(1.5, 1.0));   // ties round away from zero
        Assert.Null(p.GetPoint(0.0, 0.0));            // outside x bounds
        Assert.Equal((3, 0), p.GetPoint(2.0, 2.0));
        Assert.Equal((0, 0), p.GetPoint(1.0, 2.0));
    }
    #endregion

    #region Per-marker line grids (canvas.rs test_horizontal_with_vertical / test_diagonal_lines)
    [Theory]
    [InlineData(CanvasMarker.Block, ' ', "█xxxx\n█xxxx\n█xxxx\n█xxxx\n█████")]
    [InlineData(CanvasMarker.HalfBlock, ' ', "█xxxx\n█xxxx\n█xxxx\n█xxxx\n█▄▄▄▄")]
    [InlineData(CanvasMarker.Bar, ' ', "▄xxxx\n▄xxxx\n▄xxxx\n▄xxxx\n▄▄▄▄▄")]
    [InlineData(CanvasMarker.Braille, ' ', "⡇xxxx\n⡇xxxx\n⡇xxxx\n⡇xxxx\n⣇⣀⣀⣀⣀")]
    [InlineData(CanvasMarker.Quadrant, ' ', "▌xxxx\n▌xxxx\n▌xxxx\n▌xxxx\n▙▄▄▄▄")]
    [InlineData(CanvasMarker.Custom, '×', "×xxxx\n×xxxx\n×xxxx\n×xxxx\n×××××")]
    [InlineData(CanvasMarker.Custom, '+', "+xxxx\n+xxxx\n+xxxx\n+xxxx\n+++++")]
    [InlineData(CanvasMarker.Dot, ' ', "•xxxx\n•xxxx\n•xxxx\n•xxxx\n•••••")]
    public void Marker_HorizontalAndVerticalLine(CanvasMarker marker, char custom, string expected)
    {
        var canvas = new Canvas().WithXBounds(0, 10).WithYBounds(0, 10).WithMarker(marker);
        if (marker == CanvasMarker.Custom) canvas.CustomMarker = custom;
        canvas.Add(new Line(0, 0, 0, 10, White));   // vertical
        canvas.Add(new Line(0, 0, 10, 0, White));   // horizontal

        Assert.Equal(Expected(expected), ConsoleSnapshot.ToText(canvas, 5, 5));
    }

    [Theory]
    [InlineData(CanvasMarker.Block, ' ', "█xxx█\nx█x█x\nxx█xx\nx█x█x\n█xxx█")]
    [InlineData(CanvasMarker.HalfBlock, ' ', "█xxx█\nx█x█x\nxx█xx\nx█x█x\n█xxx█")]
    [InlineData(CanvasMarker.Bar, ' ', "▄xxx▄\nx▄x▄x\nxx▄xx\nx▄x▄x\n▄xxx▄")]
    [InlineData(CanvasMarker.Braille, ' ', "⢣xxx⡜\nx⢣x⡜x\nxx⣿xx\nx⡜x⢣x\n⡜xxx⢣")]
    [InlineData(CanvasMarker.Quadrant, ' ', "▚xxx▞\nx▚x▞x\nxx█xx\nx▞x▚x\n▞xxx▚")]
    [InlineData(CanvasMarker.Custom, '×', "×xxx×\nx×x×x\nxx×xx\nx×x×x\n×xxx×")]
    [InlineData(CanvasMarker.Custom, '+', "+xxx+\nx+x+x\nxx+xx\nx+x+x\n+xxx+")]
    [InlineData(CanvasMarker.Dot, ' ', "•xxx•\nx•x•x\nxx•xx\nx•x•x\n•xxx•")]
    public void Marker_DiagonalLines(CanvasMarker marker, char custom, string expected)
    {
        var canvas = new Canvas().WithXBounds(0, 10).WithYBounds(0, 10).WithMarker(marker);
        if (marker == CanvasMarker.Custom) canvas.CustomMarker = custom;
        canvas.Add(new Line(0, 10, 10, 0, White));   // diagonal down
        canvas.Add(new Line(0, 0, 10, 10, White));   // diagonal up

        Assert.Equal(Expected(expected), ConsoleSnapshot.ToText(canvas, 5, 5));
    }
    #endregion

    #region Rectangle (rectangle.rs draw_braille_lines)
    [Fact]
    public void Rectangle_NestedBraille()
    {
        var canvas = new Canvas().WithXBounds(0, 20).WithYBounds(0, 20).WithMarker(CanvasMarker.Braille);
        canvas.Add(new Rectangle(0, 0, 20, 20, White));    // outer
        canvas.Add(new Rectangle(4, 4, 12, 12, White));    // inner

        var expected = Expected(
            "⡏⠉⠉⠉⠉⠉⠉⠉⠉⢹\n" +
            "⡇        ⢸\n" +
            "⡇ ⡏⠉⠉⠉⠉⢹ ⢸\n" +
            "⡇ ⡇    ⢸ ⢸\n" +
            "⡇ ⡇    ⢸ ⢸\n" +
            "⡇ ⡇    ⢸ ⢸\n" +
            "⡇ ⡇    ⢸ ⢸\n" +
            "⡇ ⣇⣀⣀⣀⣀⣸ ⢸\n" +
            "⡇        ⢸\n" +
            "⣇⣀⣀⣀⣀⣀⣀⣀⣀⣸");

        Assert.Equal(expected, ConsoleSnapshot.ToText(canvas, 10, 10));
    }
    #endregion

    #region Circle (circle.rs test_it_draws_a_circle)
    [Fact]
    public void Circle_Braille()
    {
        var canvas = new Canvas().WithXBounds(-10, 10).WithYBounds(-10, 10).WithMarker(CanvasMarker.Braille);
        canvas.Add(new Circle(5, 2, 5, White));

        var expected = Expected(
            "      ⣀⣀⣀ \n" +
            "     ⡞⠁ ⠈⢣\n" +
            "     ⢇⡀ ⢀⡼\n" +
            "      ⠉⠉⠉ \n" +
            "          ");

        Assert.Equal(expected, ConsoleSnapshot.ToText(canvas, 10, 5));
    }
    #endregion

    #region Line geometry (line.rs tests, Marker::Dot on a 10×10 grid)
    [Theory]
    // horizontal along the bottom row
    [InlineData(0, 0, 10, 0, "\n\n\n\n\n\n\n\n\n••••••••••")]
    // vertical up the left column
    [InlineData(0, 0, 0, 10, "•\n•\n•\n•\n•\n•\n•\n•\n•\n•")]
    // dy < dx, x1 < x2
    [InlineData(0, 0, 10, 5, "\n\n\n\n\n        ••\n      ••  \n    ••    \n  ••      \n••        ")]
    // dy > dx, y1 < y2
    [InlineData(0, 0, 5, 10, "     •    \n    •     \n    •     \n   •      \n   •      \n  •       \n  •       \n •        \n •        \n•         ")]
    public void Line_DotGeometry(double x1, double y1, double x2, double y2, string expected)
    {
        var canvas = new Canvas().WithXBounds(0, 10).WithYBounds(0, 10).WithMarker(CanvasMarker.Dot);
        canvas.Add(new Line(x1, y1, x2, y2, White));

        Assert.Equal(Expected(expected), ConsoleSnapshot.ToText(canvas, 10, 10));
    }

    [Fact]
    public void Line_ClipsPartiallyOffGridSegment()
    {
        // A line entirely off the left edge that clips into the grid along the bottom row (line.rs off_grid5).
        var canvas = new Canvas().WithXBounds(0, 10).WithYBounds(0, 10).WithMarker(CanvasMarker.Dot);
        canvas.Add(new Line(-10, 0, 5, 0, White));

        Assert.Equal(Expected("\n\n\n\n\n\n\n\n\n••••••"), ConsoleSnapshot.ToText(canvas, 10, 10));
    }

    [Fact]
    public void Line_WhollyOffGrid_DrawsNothing()
    {
        var canvas = new Canvas().WithXBounds(0, 10).WithYBounds(0, 10).WithMarker(CanvasMarker.Dot);
        canvas.Add(new Line(-1, 0, -1, 10, White));   // line.rs off_grid1

        Assert.Equal(Expected("\n\n\n\n\n\n\n\n\n"), ConsoleSnapshot.ToText(canvas, 10, 10));
    }
    #endregion

    #region FilledLine geometry (line.rs tests_filled, Marker::Dot)
    [Fact]
    public void FilledLine_DiagonalFillsUnderTheLine()
    {
        // filled_diagonal1: (0,0)-(10,5) filled down to y=0.
        var canvas = new Canvas().WithXBounds(0, 10).WithYBounds(0, 10).WithMarker(CanvasMarker.Dot);
        canvas.Add(new FilledLine(0, 0, 10, 5, 0, White));

        var expected = Expected(
            "\n\n\n\n\n" +
            "        ••\n" +
            "      ••••\n" +
            "    ••••••\n" +
            "  ••••••••\n" +
            "••••••••••");

        Assert.Equal(expected, ConsoleSnapshot.ToText(canvas, 10, 10));
    }

    [Fact]
    public void FilledLine_SplitsFillAboveAndBelow()
    {
        // filled_split1: (0,0)-(10,10) filled to y=5 — fills up to and down to the mid row.
        var canvas = new Canvas().WithXBounds(0, 10).WithYBounds(0, 10).WithMarker(CanvasMarker.Dot);
        canvas.Add(new FilledLine(0, 0, 10, 10, 5, White));

        var expected = Expected(
            "         •\n" +
            "        ••\n" +
            "       •••\n" +
            "      ••••\n" +
            "     •••••\n" +
            "••••••••••\n" +
            "••••      \n" +
            "•••       \n" +
            "••        \n" +
            "•         ");

        Assert.Equal(expected, ConsoleSnapshot.ToText(canvas, 10, 10));
    }
    #endregion

    #region Layers, points, robustness
    [Fact]
    public void Layers_ComposeInOrder_NonOverlappingCellsBothSurvive()
    {
        // Two layers (same marker) each paint a different corner; both survive compositing, each keeping its colour.
        var a = new CColor(240, 80, 80);
        var b = new CColor(80, 160, 240);
        var canvas = new Canvas().WithXBounds(0, 10).WithYBounds(0, 10).WithMarker(CanvasMarker.Dot);
        canvas.Add(new Points([(0, 0)], a));    // layer 1 → bottom-left, buffer cell (0,9)
        canvas.Layer();
        canvas.Add(new Points([(10, 10)], b));  // layer 2 → top-right, buffer cell (9,0)

        var buf = ConsoleSnapshot.Render(canvas, 10, 10);
        Assert.Equal(a, buf[0, 9].Character.Foreground);
        Assert.Equal(b, buf[9, 0].Character.Foreground);
    }

    [Fact]
    public void Layers_TopLayerWinsWhereCellsOverlap()
    {
        var lower = new CColor(240, 80, 80);
        var upper = new CColor(80, 160, 240);
        var canvas = new Canvas().WithXBounds(0, 10).WithYBounds(0, 10).WithMarker(CanvasMarker.Dot);
        canvas.Add(new Points([(0, 0)], lower));   // layer 1
        canvas.Layer();
        canvas.Add(new Points([(0, 0)], upper));   // layer 2, same cell → wins

        var buf = ConsoleSnapshot.Render(canvas, 10, 10);
        Assert.Equal(upper, buf[0, 9].Character.Foreground);
    }

    [Fact]
    public void Points_ScatterPlotsEachCoordinate()
    {
        var canvas = new Canvas().WithXBounds(0, 10).WithYBounds(0, 10).WithMarker(CanvasMarker.Dot);
        canvas.Add(new Points([(0, 0), (10, 10)], White));   // bottom-left and top-right corners

        var text = ConsoleSnapshot.ToText(canvas, 10, 10);
        var lines = text.Split('\n');
        Assert.Equal("         •", lines[0]);   // top-right
        Assert.Equal("•", lines[9]);            // bottom-left (row 9), trailing spaces trimmed
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(1, 1)]
    [InlineData(2, 1)]
    public void Render_TinyOrZeroSize_DoesNotThrow(int w, int h)
    {
        var canvas = new Canvas().WithXBounds(0, 10).WithYBounds(0, 10);
        canvas.Add(new Rectangle(0, 0, 10, 10, White));
        canvas.Add(new Circle(5, 5, 4, White));

        var ex = Record.Exception(() => ConsoleSnapshot.ToText(canvas, w, h));
        Assert.Null(ex);
    }

    [Fact]
    public void EmptyCanvas_RendersBlank()
    {
        var canvas = new Canvas().WithXBounds(0, 10).WithYBounds(0, 10);
        Assert.Equal(Expected("\n\n\n\n"), ConsoleSnapshot.ToText(canvas, 5, 5));
    }

    [Fact]
    public void Background_FillsBehindCells()
    {
        var canvas = new Canvas().WithXBounds(0, 10).WithYBounds(0, 10).WithBackground(new CColor(10, 20, 30));
        var buf = ConsoleSnapshot.Render(canvas, 3, 3);

        for (var y = 0; y < 3; y++)
            for (var x = 0; x < 3; x++)
                Assert.Equal(new CColor(10, 20, 30), buf[x, y].Character.Background);
    }
    #endregion
}
