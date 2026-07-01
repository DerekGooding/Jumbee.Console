namespace Jumbee.Console.Tests;

using ConsoleGUI.Space;

using Jumbee.Console;
using Jumbee.Console.Snapshot;

using Xunit;

/// <summary>Tests for the vertical scrollbar rendering: the modern smooth (Textual-style, sub-cell block) bar that
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

    #region Log content-sizing + scrolling
    [Fact]
    public void Log_ShortContent_IsContentSized_NoScrollbar()
    {
        // A one-line log must be content-sized (one row), not balloon to the 1000-row scroll clamp (which drew a
        // full-height track with a tiny thumb over empty rows).
        var log = new Log();
        log.Write("just one line");
        log.WithRoundedBorder();
        ConsoleSnapshot.Render(log, 40, 12);

        // Fits the viewport (12 − 2 border rows), not ballooned to the 1000-row scroll clamp -> no scrollbar.
        Assert.True(log.ActualHeight is > 0 and <= 10, $"expected a viewport-sized height, got {log.ActualHeight}");
        Assert.Equal(-1, FirstThumbRow(log.Frame!));   // no scrollbar
    }

    [Fact]
    public void Log_LongContent_Overflows_AndShowsScrollbar()
    {
        var log = new Log();
        for (var i = 1; i <= 40; i++) log.Write($"line {i:00}");
        log.WithRoundedBorder();
        ConsoleSnapshot.Render(log, 40, 12);
        log.Frame!.Top = 15;

        Assert.Equal(40, log.ActualHeight);         // content-sized to all entries -> the frame can scroll it
        Assert.True(FirstThumbRow(log.Frame!) >= 0, "an overflowing log should show a scrollbar");
    }

    [Fact]
    public void Log_Write_TailsToBottom()
    {
        var log = new Log();
        for (var i = 1; i <= 4; i++) log.Write($"line {i}");
        log.WithRoundedBorder();
        ConsoleSnapshot.Render(log, 40, 8);         // lay out first (viewport ~6); 4 entries fit -> Top 0
        Assert.Equal(0, log.Frame!.Top);

        for (var i = 5; i <= 20; i++) log.Write($"line {i}");   // now overflows; each Write tails to the bottom

        Assert.True(log.Frame!.Top > 0, "writing past the viewport should auto-scroll the frame to the bottom");
    }

    [Fact]
    public void Log_AltUpDown_ScrollsTheFrame()
    {
        var log = new Log();
        for (var i = 1; i <= 40; i++) log.Write($"line {i:00}");
        log.WithRoundedBorder();
        ConsoleSnapshot.Render(log, 40, 12);
        log.Frame!.Top = 0;

        UI.SendInput(log, UI.HotKeys.AltDown);   // Alt+Down scrolls the wrapping frame down
        Assert.True(log.Frame!.Top > 0, "Alt+Down should scroll the frame down");

        var afterDown = log.Frame!.Top;
        UI.SendInput(log, UI.HotKeys.AltUp);     // Alt+Up scrolls back up
        Assert.True(log.Frame!.Top < afterDown, "Alt+Up should scroll the frame up");
    }
    #endregion
}
