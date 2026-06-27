namespace Jumbee.Console.Tests;

using System;

using ConsoleGUI;

using Jumbee.Console;
using Jumbee.Console.Snapshot;

using Xunit;

public class ListBoxTests
{
    // True if any cell on row y carries a background colour (the selection highlight, since unselected rows draw none).
    private static bool RowHasBackground(ConsoleBuffer buf, int y)
    {
        for (var x = 0; x < buf.Size.Width; x++)
            if (buf[x, y].Background is not null) return true;
        return false;
    }

    [Fact]
    public void BareListBox_ShowsAVisibleSelection_FromTheTheme()
    {
        // A user-observable check: a ListBox with NO explicit colours must still highlight the selected row, so the
        // selection is visible (this defaults from the theme; it was previously invisible until colours were set).
        var list = new ListBox("alpha", "beta", "gamma");   // selection defaults to row 0

        var buf = ConsoleSnapshot.Render(list, 12, 3);

        Assert.True(RowHasBackground(buf, 0));    // the selected row is highlighted out of the box
        Assert.False(RowHasBackground(buf, 1));   // an unselected row is not
    }

    [Fact]
    public void BareListBox_MovesTheVisibleHighlight_OnDownArrow()
    {
        var list = new ListBox("alpha", "beta", "gamma");

        var buf = ConsoleSnapshot.RenderAfter(list, 12, 3, ConsoleKey.DownArrow);

        Assert.False(RowHasBackground(buf, 0));   // highlight left the first row
        Assert.True(RowHasBackground(buf, 1));    // and is now visibly on the second
    }
}
