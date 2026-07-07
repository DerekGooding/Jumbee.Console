namespace Jumbee.Console.Tests;

using ConsoleGUI.Space;

using Jumbee.Console;
using Jumbee.Console.Snapshot;

using Xunit;

/// <summary>Tests for the vertical scrollbar rendering: the modern smooth bar that
/// is now the default, and the classic three-part glyph bar retained for legacy terminals.</summary>
public class ScrollBarTests
{
    // The default thumb colour (ScrollBarStyle.Default) — the smooth bar's thumb is this colour, the track is darker.
    private const byte ThumbRed = 158;

    private static ListBox TallList(int n = 40)
    {
        var lb = new ListBox();
        for (var i = 1; i <= n; i++) lb.AddItem($"Item {i:00}");
        return lb;
    }

    // The scrollbar sits at the right edge of the viewport, just inside the 1-cell border (column width-2).
    private static int ScrollCol(ControlFrame frame) => frame.Size.Width - 2;

    // Reads the scrollbar column top-to-bottom as a glyph string.
    private static string ScrollColumn(ControlFrame frame)
    {
        var x = ScrollCol(frame);
        var sb = new System.Text.StringBuilder();
        for (var y = 0; y < frame.Size.Height; y++) sb.Append(frame[new Position(x, y)].Character.Content ?? ' ');
        return sb.ToString();
    }

    // A smooth-bar cell belongs to the thumb when the thumb colour appears as its foreground (a full/lower block) or
    // its background (the inverted top-edge cell). Track cells are the darker colour only.
    private static bool IsThumb(ControlFrame frame, int y)
    {
        var c = frame[new Position(ScrollCol(frame), y)].Character;
        return c.Foreground?.Red == ThumbRed || c.Background?.Red == ThumbRed;
    }

    private static int FirstThumbRow(ControlFrame frame)
    {
        for (var y = 0; y < frame.Size.Height; y++) if (IsThumb(frame, y)) return y;
        return -1;
    }

    #region Smooth (default)
    [Fact]
    public void Default_IsSmoothBlockBar_NoArrows()
    {
        var lb = TallList();
        lb.WithRoundedBorder();
        ConsoleSnapshot.Render(lb, 20, 12);
        lb.Frame!.Top = 0;

        var col = ScrollColumn(lb.Frame!);

        Assert.True(FirstThumbRow(lb.Frame!) >= 0);   // a thumb is present (solid cells are bg-filled spaces)...
        Assert.DoesNotContain('▲', col);              // ...and no end arrows (only the classic bar has those)
        Assert.DoesNotContain('▼', col);
    }

    [Fact]
    public void Smooth_SolidCells_AreSeamlessBackgroundFills_NotBlockGlyphs()
    {
        // Regression: full thumb/track cells must be a background-filled space (not the '█' glyph), so the bar
        // renders as a solid bar on terminals whose font draws stacked full blocks with vertical seams.
        var lb = TallList();
        lb.WithRoundedBorder();
        ConsoleSnapshot.Render(lb, 20, 12);
        lb.Frame!.Top = 14;

        var col = ScrollColumn(lb.Frame!);
        Assert.DoesNotContain('█', col);   // no full-block glyph anywhere — solid cells are bg-filled instead

        // A full (non-edge) thumb/track cell is a space with the colour in the background (no foreground glyph).
        var x = ScrollCol(lb.Frame!);
        var solid = 0;
        for (var y = 1; y < lb.Frame!.Size.Height - 1; y++)
        {
            var cell = lb.Frame![new Position(x, y)].Character;
            if (cell.Content == ' ' && cell.Background is { } bg && (bg.Red == ThumbRed || bg.Red == 68)) solid++;
        }
        Assert.True(solid > 0, "expected some solid background-filled scrollbar cells");
    }

    [Fact]
    public void Smooth_ThumbMovesDown_AsContentScrolls()
    {
        var lb = TallList();
        lb.WithRoundedBorder();
        ConsoleSnapshot.Render(lb, 20, 12);

        lb.Frame!.Top = 0;
        var topThumb = FirstThumbRow(lb.Frame!);   // at the first viewport row (row 0 is the border)

        lb.Frame!.Top = 30;                        // scroll near the bottom
        var bottomThumb = FirstThumbRow(lb.Frame!);

        Assert.True(topThumb >= 0, "thumb should be present");
        Assert.True(bottomThumb > topThumb, $"thumb should slide down (top={topThumb}, bottom={bottomThumb})");
    }

    [Fact]
    public void Smooth_UsesEighthBlocks_AtTheThumbEdge()
    {
        // A fractional scroll position should render at least one sub-cell eighth-block, not only full cells.
        var lb = TallList(41);                  // odd count -> fractional thumb geometry
        lb.WithRoundedBorder();
        ConsoleSnapshot.Render(lb, 20, 12);
        lb.Frame!.Top = 5;

        var col = ScrollColumn(lb.Frame!);
        Assert.Contains(col, c => "▁▂▃▄▅▆▇".Contains(c));
    }
    #endregion

    #region Classic (legacy)
    [Fact]
    public void Classic_KeepsArrowsAndGlyphThumb()
    {
        var lb = TallList();
        lb.WithRoundedBorder().WithScrollBarGlyphs(ScrollBarGlyphs.Classic);
        ConsoleSnapshot.Render(lb, 20, 12);
        lb.Frame!.Top = 10;

        var col = ScrollColumn(lb.Frame!);

        Assert.Contains('▲', col);   // up arrow at the top
        Assert.Contains('▼', col);   // down arrow at the bottom
        Assert.Contains('#', col);   // the classic '#' thumb
    }
    #endregion

    #region Log self-scrolling (viewport-virtualized; owns its scroll + scrollbar)
    private static ConsoleKeyInfo Key(ConsoleKey k) => new('\0', k, false, false, false);

    // The virtualized Log draws its own scrollbar in its rightmost column; the thumb is the '█' block.
    private static bool LogHasThumb(Log log)
    {
        var x = log.ActualWidth - 1;
        for (var y = 0; y < log.ActualHeight; y++)
            if (log[new Position(x, y)].Character.Content == '█') return true;
        return false;
    }

    [Fact]
    public void Log_ShortContent_FillsViewport_NoScrollbar()
    {
        // A short log fills the framing viewport (it owns its scrolling) rather than ballooning content-tall; with
        // everything visible there's no scrollbar — neither the frame's nor the log's own.
        var log = new Log();
        log.Write("just one line");
        log.WithRoundedBorder();
        ConsoleSnapshot.Render(log, 40, 12);

        Assert.True(log.ActualHeight is > 0 and <= 10, $"expected a viewport-sized height, got {log.ActualHeight}");
        Assert.Equal(-1, FirstThumbRow(log.Frame!));   // frame draws no scrollbar for a fill-viewport control
        Assert.False(LogHasThumb(log), "a fully-visible log should draw no scrollbar thumb");
    }

    [Fact]
    public void Log_LongContent_DrawsOwnScrollbar()
    {
        var log = new Log();
        for (var i = 1; i <= 40; i++) log.Write($"line {i:00}");
        ConsoleSnapshot.Render(log, 40, 12);

        Assert.Equal(12, log.ActualHeight);   // fills the viewport, not content-sized to 40
        Assert.True(LogHasThumb(log), "an overflowing log should draw its own scrollbar thumb");
    }

    [Fact]
    public void Log_Write_TailsToNewest()
    {
        var log = new Log();
        for (var i = 1; i <= 20; i++) log.Write($"L{i:000}");   // more than fits a 6-row viewport

        var text = ConsoleSnapshot.ToText(log, 40, 6);
        Assert.Contains("L020", text);          // newest is visible (tailed)
        Assert.DoesNotContain("L001", text);    // oldest scrolled off the top
    }

    [Fact]
    public void Log_ScrollKeys_ShowOlderContent_AndEndReturnsToTail()
    {
        var log = new Log();
        for (var i = 1; i <= 40; i++) log.Write($"L{i:000}");
        ConsoleSnapshot.Render(log, 40, 12);
        Assert.Contains("L040", ConsoleSnapshot.ToText(log, 40, 12));   // starts tailing -> newest visible

        UI.SendInput(log, Key(ConsoleKey.PageUp));                     // scroll up into history
        Assert.DoesNotContain("L040", ConsoleSnapshot.ToText(log, 40, 12));

        UI.SendInput(log, Key(ConsoleKey.End));                        // End re-engages tailing
        Assert.Contains("L040", ConsoleSnapshot.ToText(log, 40, 12));
    }

    [Fact]
    public void Log_WriteWhileScrolledUp_KeepsViewPut()
    {
        var log = new Log();
        for (var i = 1; i <= 40; i++) log.Write($"L{i:000}");
        ConsoleSnapshot.Render(log, 40, 12);

        UI.SendInput(log, Key(ConsoleKey.Home));                       // scroll to the very top
        var top = ConsoleSnapshot.ToText(log, 40, 12);
        Assert.Contains("L001", top);

        for (var i = 41; i <= 50; i++) log.Write($"L{i:000}");         // new writes must NOT yank the view down
        Assert.Contains("L001", ConsoleSnapshot.ToText(log, 40, 12));
        Assert.DoesNotContain("L050", ConsoleSnapshot.ToText(log, 40, 12));
    }
    #endregion
}
