namespace Jumbee.Console.Tests;

using ConsoleGUI;

using Jumbee.Console;
using Jumbee.Console.Snapshot;

using Xunit;

public class TextEditorTests
{
    // Locates the cell the editor flagged as the terminal cursor (Character.IsCursor).
    private static (int x, int y)? FindCursor(ConsoleBuffer buf)
    {
        for (var y = 0; y < buf.Size.Height; y++)
            for (var x = 0; x < buf.Size.Width; x++)
                if (buf[x, y].Character.IsCursor) return (x, y);
        return null;
    }

    #region Line-ending normalization
    [Fact]
    public void Text_NormalizesWindowsLineEndings()
    {
        var ed = new TextEditor { Text = "a\r\nb\rc" };

        Assert.Equal("a\nb\nc", ed.Text);
        Assert.Equal(3, ed.LineCount);
        Assert.DoesNotContain('\r', ed.Text);
    }

    [Fact]
    public void Paste_NormalizesLineEndings()
    {
        var ed = new TextEditor();
        ed.OnPaste("x\r\ny");

        Assert.Equal("x\ny", ed.Text);
        Assert.DoesNotContain('\r', ed.Text);
    }
    #endregion

    #region Tab key (indent)
    [Fact]
    public void Tab_InsertsTabWidthSpaces()
    {
        var ed = new TextEditor();   // empty, caret at start

        UI.SendInput(ed, new ConsoleKeyInfo('\t', ConsoleKey.Tab, false, false, false));

        Assert.Equal("    ", ed.Text);   // default TabWidth = 4
    }

    [Fact]
    public void Tab_RespectsTabWidth_AndInsertsAtCaret()
    {
        var ed = new TextEditor { TabWidth = 2 };

        UI.SendInput(ed, new ConsoleKeyInfo('\t', ConsoleKey.Tab, false, false, false));
        UI.SendInput(ed, new ConsoleKeyInfo('x', ConsoleKey.X, false, false, false));

        Assert.Equal("  x", ed.Text);   // 2 spaces then the typed char
    }
    #endregion

    #region Cursor drawing / tracking
    [Fact]
    public void Cursor_DrawnAtCaretPosition()
    {
        var ed = new TextEditor();
        ed.Focus();
        ed.Text = "ab\ncd";   // caret ends after 'd' on line 1

        Assert.Equal((2, 1), FindCursor(ConsoleSnapshot.Render(ed, 20, 5)));
    }

    [Fact]
    public void Cursor_TracksLeftArrowNavigation()
    {
        var ed = new TextEditor();
        ed.Focus();
        ed.Text = "abc";   // caret at (3,0)

        var cursor = FindCursor(ConsoleSnapshot.RenderAfter(ed, 20, 3, ConsoleKey.LeftArrow));

        Assert.Equal((2, 0), cursor);   // moved one cell left, in sync with the caret
    }

    [Fact]
    public void Cursor_NoStallAtPastedLineBoundary()
    {
        // Before the CRLF fix a '\r' sat between the lines; the first Left press landed on it (invisible) so the
        // cursor didn't move. After normalization every Left moves the cursor.
        var ed = new TextEditor();
        ed.Focus();
        ed.OnPaste("ab\r\ncd");   // -> "ab\ncd", caret after 'd' at (2,1)

        var afterOneLeft = FindCursor(ConsoleSnapshot.RenderAfter(ed, 20, 5, ConsoleKey.LeftArrow));

        Assert.Equal((1, 1), afterOneLeft);   // lands before 'd', not stuck on a phantom '\r'
    }
    #endregion

    #region Soft wrap
    [Fact]
    public void Wrap_LongLine_RendersOnMultipleRows()
    {
        var ed = new TextEditor { Text = new string('x', 30) };

        var rows = ConsoleSnapshot.ToText(ed, 20, 4).TrimEnd('\n').Split('\n');

        Assert.Equal(20, rows[0].Length);   // first visual row filled to the width
        Assert.Equal(10, rows[1].Length);   // the remaining 10 chars wrap onto the second
    }

    [Fact]
    public void Wrap_CaretMidWrappedLine_OnSecondVisualRow()
    {
        var ed = new TextEditor();
        ed.Focus();
        ed.Text = new string('x', 40);   // caret at 40; 5 Lefts -> index 35 -> column 15 of visual row 1

        var cursor = FindCursor(ConsoleSnapshot.RenderAfter(ed, 20, 6,
            ConsoleKey.LeftArrow, ConsoleKey.LeftArrow, ConsoleKey.LeftArrow, ConsoleKey.LeftArrow, ConsoleKey.LeftArrow));

        Assert.Equal((15, 1), cursor);   // tracks the wrapped row, not off-screen at x=35
    }

    [Fact]
    public void Wrap_CaretAtEndOfFullRow_ShowsAtStartOfNextVisualRow()
    {
        var ed = new TextEditor();
        ed.Focus();
        ed.Text = new string('x', 40);   // two full 20-wide visual rows; caret at the very end

        Assert.Equal((0, 2), FindCursor(ConsoleSnapshot.Render(ed, 20, 6)));
    }
    #endregion

    #region Desired column (vertical navigation)
    [Fact]
    public void DesiredColumn_PreservedAcrossVerticalMoves()
    {
        var ed = new TextEditor();
        ed.Focus();
        ed.Text = "aaaaaaaa\nbb\ncccccccc";   // lines of width 8, 2, 8; caret ends at (8,2)

        // Up onto the short middle line clamps to column 2, but the desired column (8) is remembered, so a second
        // Up returns to column 8 on the first line — not column 2.
        var cursor = FindCursor(ConsoleSnapshot.RenderAfter(ed, 20, 4, ConsoleKey.UpArrow, ConsoleKey.UpArrow));

        Assert.Equal((8, 0), cursor);
    }
    #endregion
}
