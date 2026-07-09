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
    public void Layers_PerLayerMarkers_BlockBackgroundShowsUnderBrailleGlyph()
    {
        // A Block layer paints a cell background; a later Braille layer paints the glyph + foreground on the SAME
        // cell. Per-property compositing keeps the block's background under the braille glyph — the point of
        // per-layer markers.
        var block = new CColor(30, 60, 90);
        var braille = new CColor(200, 200, 200);
        var canvas = new Canvas().WithXBounds(0, 2).WithYBounds(0, 4).WithMarker(CanvasMarker.Block);

        canvas.Add(new Points([(0, 0)], block));            // layer 1 (block): sets fg + bg
        canvas.Layer(CanvasMarker.Braille);                 // switch marker for the next layer
        canvas.Add(new Points([(0, 0)], braille));          // layer 2 (braille): sets glyph + fg only

        var cell = ConsoleSnapshot.Render(canvas, 1, 1)[0, 0].Character;
        Assert.NotEqual('█', cell.Content);                                 // a braille glyph is on top, not the block
        Assert.True(cell.Content >= '⠀' && cell.Content <= '⣿');
        Assert.Equal(braille, cell.Foreground);                             // braille layer's foreground
        Assert.Equal(block, cell.Background);                               // block layer's background shows through
    }

    [Fact]
    public void Layers_PerLayerMarkers_EachLayerRendersItsOwnGlyphSet()
    {
        // Two layers, two markers: a Block corner and a Braille corner each render with their own glyph.
        var canvas = new Canvas().WithXBounds(0, 10).WithYBounds(0, 10).WithMarker(CanvasMarker.Block);
        canvas.Add(new Points([(0, 0)], White));            // block → bottom-left, buffer cell (0,9)
        canvas.Layer(CanvasMarker.Braille);
        canvas.Add(new Points([(10, 10)], White));          // braille → top-right, buffer cell (9,0)

        var buf = ConsoleSnapshot.Render(canvas, 10, 10);
        Assert.Equal('█', buf[0, 9].Character.Content);                     // block glyph
        var tr = buf[9, 0].Character.Content;
        Assert.True(tr >= '⠀' && tr <= '⣿');                                // braille glyph
    }

    [Fact]
    public void Layers_EmptyMarkerSwitch_DoesNotCreateAStrayLayer()
    {
        // Switching marker before drawing anything must not flush an empty layer that could blank cells.
        var canvas = new Canvas().WithXBounds(0, 10).WithYBounds(0, 10).WithMarker(CanvasMarker.Block);
        canvas.Layer(CanvasMarker.Braille);                 // no draws yet on the block grid
        canvas.Add(new Points([(0, 0)], White));

        var buf = ConsoleSnapshot.Render(canvas, 10, 10);
        var c = buf[0, 9].Character.Content;
        Assert.True(c >= '⠀' && c <= '⣿');                                  // the braille point rendered
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
    public void FramedCanvas_InGrid_RendersShapes()
    {
        // The markers-comparison demo composes framed canvases in a Grid; verify that path — a framed Canvas in a
        // grid cell renders its shapes (fills the framed viewport rather than ballooning or blanking).
        var canvas = new Canvas().WithMarker(CanvasMarker.Braille).WithXBounds(-1.1, 1.1).WithYBounds(-1.1, 1.1);
        canvas.Add(new Circle(0, 0, 1.0, White));
        var grid = new Grid([12], [24], [[canvas.WithFrame(BorderStyle.Rounded, title: "C")]]);

        var text = ConsoleSnapshot.ToText(grid, 26, 14);
        Assert.Contains("C", text);                                   // frame title drawn
        Assert.True(text.IndexOfAny(BrailleRange) >= 0, "the circle should render braille inside the framed grid cell");
    }

    // Every braille glyph, for presence checks.
    private static readonly char[] BrailleRange = BuildBrailleRange();
    private static char[] BuildBrailleRange()
    {
        var a = new char['⣿' - '⠀' + 1];
        for (int i = 0; i < a.Length; i++) a[i] = (char)('⠀' + i);
        return a;
    }

    #region Labels (canvas.rs Context.print)
    [Fact]
    public void Print_DrawsLabelText_OnTopOfShapes()
    {
        var canvas = new Canvas().WithXBounds(0, 10).WithYBounds(0, 10).WithMarker(CanvasMarker.Dot);
        canvas.Add(new Points([(5, 5)], White));
        canvas.Print(0, 10, "Hi", White);   // top-left corner

        var text = ConsoleSnapshot.ToText(canvas, 20, 10);
        Assert.StartsWith("Hi", text.Split('\n')[0]);   // label runs right from the top-left cell
    }

    [Fact]
    public void Print_OutOfBoundsLabel_IsNotDrawn()
    {
        var canvas = new Canvas().WithXBounds(0, 10).WithYBounds(0, 10);
        canvas.Print(20, 20, "off", White);   // outside the coordinate window

        Assert.DoesNotContain("off", ConsoleSnapshot.ToText(canvas, 20, 10));
    }

    [Fact]
    public void Print_ClipsAtRightEdge()
    {
        var canvas = new Canvas().WithXBounds(0, 10).WithYBounds(0, 10);
        canvas.Print(10, 10, "ABCDE", White);   // top-right corner: only the first cell is on-buffer

        var top = ConsoleSnapshot.ToText(canvas, 6, 4).Split('\n')[0];
        Assert.Equal("     A", top);   // 'A' at the last column, the rest clipped
    }
    #endregion

    #region World map (map.rs)
    [Fact]
    public void WorldMap_Data_HasExpectedCountsAndEndpoints()
    {
        var low = WorldMapData.Get(MapResolution.Low);
        var high = WorldMapData.Get(MapResolution.High);

        Assert.Equal(1166, low.Length);
        Assert.Equal(5125, high.Length);

        // Exact endpoints from ratatui's world.rs — proves the embedded data's order and (lon, lat) pairing.
        Assert.Equal((-92.32, 48.24), low[0]);
        Assert.Equal((66.82, 66.82), low[^1]);
        Assert.Equal((-163.7128, -78.5956), high[0]);
        Assert.Equal((180.0, -84.71338), high[^1]);
    }

    [Fact]
    public void WorldMap_Data_AllPointsWithinGeographicRange()
    {
        // Latitude is strictly geographic; longitude runs a touch past +180° in the source data (Russia's far east
        // across the date line), and those points simply clip out when the canvas bounds are [-180, 180]. The tight
        // latitude bound is what would catch a lon/lat swap in the extraction.
        foreach (var res in new[] { MapResolution.Low, MapResolution.High })
            foreach (var (lon, lat) in WorldMapData.Get(res))
            {
                Assert.InRange(lon, -180.0, 189.0);
                Assert.InRange(lat, -90.0, 90.0);
            }
    }

    [Fact]
    public void WorldMap_LowDot_RendersARecognisableMap()
    {
        // The transform (GetPoint + Dot marker) is already pixel-golden-tested; combined with the exact data above,
        // the map render is correct by construction. Here we just confirm it draws a non-trivial spread of land.
        var canvas = new Canvas().WithXBounds(-180, 180).WithYBounds(-90, 90).WithMarker(CanvasMarker.Dot);
        canvas.Add(new WorldMap(White, MapResolution.Low));

        var buf = ConsoleSnapshot.Render(canvas, 80, 40);
        int dots = 0, minX = 80, maxX = -1;
        for (int y = 0; y < 40; y++)
            for (int x = 0; x < 80; x++)
                if (buf[x, y].Character.Content == '•') { dots++; if (x < minX) minX = x; if (x > maxX) maxX = x; }

        Assert.InRange(dots, 400, 1166);          // many cells, but points collide so fewer than the 1166 source points
        Assert.True(maxX - minX >= 70, "land should span most of the longitude range");
    }

    [Fact]
    public void WorldMap_HighBraille_RendersBraille()
    {
        var canvas = new Canvas().WithXBounds(-180, 180).WithYBounds(-90, 90).WithMarker(CanvasMarker.Braille);
        canvas.Add(new WorldMap(White, MapResolution.High));

        Assert.True(ConsoleSnapshot.ToText(canvas, 80, 40).IndexOfAny(BrailleRange) >= 0);
    }
    #endregion

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
