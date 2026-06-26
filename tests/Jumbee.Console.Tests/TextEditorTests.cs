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
}
