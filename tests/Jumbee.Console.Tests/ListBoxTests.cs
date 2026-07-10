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

    [Fact]
    public void CaretSelectionStyle_PrefixesSelectedRowWithTheCaretGlyph()
    {
        var list = new ListBox("alpha", "beta") { SelectionStyle = SelectionStyle.Caret };

        var rows = ConsoleSnapshot.ToText(list, 14, 2).TrimEnd('\n').Split('\n');

        Assert.Contains("▶", rows[0]);          // the selected row carries the caret glyph
        Assert.Contains("alpha", rows[0]);
        Assert.DoesNotContain("▶", rows[1]);    // an unselected row does not
    }

    [Fact]
    public void CaretSelectionStyle_ReservesGutter_SoTextDoesNotJumpWhenSelectionMoves()
    {
        var list = new ListBox("alpha", "beta") { SelectionStyle = SelectionStyle.Caret };

        // "beta" is on row 1, unselected (blank gutter).
        var before = ConsoleSnapshot.ToText(list, 16, 2).TrimEnd('\n').Split('\n');
        var colBefore = before[1].IndexOf("beta");

        // Select "beta" (now caret-prefixed). Its text column must be unchanged — the gutter was already reserved.
        var afterRows = ConsoleSnapshot.ToText(ConsoleSnapshot.RenderAfter(list, 16, 2, ConsoleKey.DownArrow))
            .TrimEnd('\n').Split('\n');
        var colAfter = afterRows[1].IndexOf("beta");

        Assert.True(colBefore > 0);            // the gutter is reserved even when unselected
        Assert.Equal(colBefore, colAfter);     // text stays put when the row becomes selected (no jump)
    }

    [Fact]
    public void UnderlineSelectionStyle_UnderlinesSelectedRow_NotOthers()
    {
        var list = new ListBox("alpha", "beta") { SelectionStyle = SelectionStyle.Underline };

        var buf = ConsoleSnapshot.Render(list, 14, 2);

        Assert.True(RowHasUnderline(buf, 0));    // selected row underlined
        Assert.False(RowHasUnderline(buf, 1));   // unselected row not
    }

    // --- Multi-line items: an item may span several rows (wrapped or newline-bearing IRenderable) ---

    [Fact]
    public void MultiLineItems_RenderAcrossMultipleRows()
    {
        var list = new ListBox(
            new Spectre.Console.Markup("[bold]Alpha[/]\nfirst desc"),
            new Spectre.Console.Markup("[bold]Beta[/]\nsecond desc"));

        var rows = ConsoleSnapshot.ToText(list, 20, 6).TrimEnd('\n').Split('\n');

        Assert.Contains("Alpha", rows[0]);
        Assert.Contains("first desc", rows[1]);    // item 0 occupies rows 0-1
        Assert.Contains("Beta", rows[2]);          // item 1 starts at row 2
        Assert.Contains("second desc", rows[3]);
    }

    [Fact]
    public void ClickInsideAMultiLineItem_SelectsThatItem()
    {
        var list = new ListBox(
            new Spectre.Console.Markup("Alpha\naaa"),
            new Spectre.Console.Markup("Beta\nbbb"));
        ConsoleSnapshot.Render(list, 20, 6);   // establish layout so the row→item map is populated
        Assert.Equal(0, list.SelectedIndex);

        // Row 3 falls inside the SECOND item (item 0 = rows 0-1, item 1 = rows 2-3).
        var ml = (ConsoleGUI.Input.IMouseListener)list;
        ml.OnMouseDown(new ConsoleGUI.Space.Position(0, 3));
        ml.OnMouseUp(new ConsoleGUI.Space.Position(0, 3));

        Assert.Equal(1, list.SelectedIndex);   // clicking the item's second row still selects that item
    }

    [Fact]
    public void SelectingATallItem_ScrollsTheFrame_ByRows()
    {
        var items = new Spectre.Console.Rendering.IRenderable[10];
        for (var i = 0; i < 10; i++) items[i] = new Spectre.Console.Markup($"[bold]Item {i}[/]\ndesc {i}");
        var list = new ListBox(items);
        list.WithRoundedBorder();   // a frame so it scrolls; content (20 rows) far exceeds the viewport

        ConsoleSnapshot.Render(list, 20, 8);
        Assert.Equal(0, list.Frame!.Top);

        list.SelectedIndex = 9;   // last item -> content rows 18-19

        Assert.True(list.Frame!.Top > 0);   // scrolled down by rows to reveal the tall item at the bottom
    }

    private static bool RowHasUnderline(ConsoleBuffer buf, int y)
    {
        for (var x = 0; x < buf.Size.Width; x++)
        {
            var d = buf[x, y].Character.Decoration;
            if (d is { } dec && (dec & ConsoleGUI.Data.Decoration.Underline) != 0) return true;
        }
        return false;
    }

    // --- IRenderable items are highlighted when selected, like Tree's IRenderable nodes ---

    [Fact]
    public void IRenderableItem_IsHighlighted_WhenSelected()
    {
        // Items added as IRenderables (not strings) must still show the selection highlight on the selected row.
        var list = new ListBox(new Spectre.Console.Text("alpha"), new Spectre.Console.Text("beta"));

        var buf = ConsoleSnapshot.Render(list, 12, 2);

        Assert.True(RowHasBackground(buf, 0));    // the selected IRenderable row is highlighted
        Assert.False(RowHasBackground(buf, 1));   // an unselected one is not
    }

    [Fact]
    public void IRenderableItem_MovesTheHighlight_OnDownArrow()
    {
        var list = new ListBox(new Spectre.Console.Text("alpha"), new Spectre.Console.Text("beta"));

        var buf = ConsoleSnapshot.RenderAfter(list, 12, 2, ConsoleKey.DownArrow);

        Assert.False(RowHasBackground(buf, 0));
        Assert.True(RowHasBackground(buf, 1));
    }

    [Fact]
    public void IRenderableItem_KeepsItsOwnColour_UnderTheHighlight()
    {
        // A colourful label stays colourful under the highlight: the overlay adds the selection background but keeps
        // each segment's own foreground (Style.Combine). So the selected green row's text foreground matches the
        // unselected green row's — only the background changes.
        var list = new ListBox(new Spectre.Console.Markup("[green]alpha[/]"), new Spectre.Console.Markup("[green]beta[/]"));

        var buf = ConsoleSnapshot.Render(list, 12, 2);   // row 0 selected, row 1 not

        Assert.True(RowHasBackground(buf, 0));                       // selected row highlighted
        Assert.False(RowHasBackground(buf, 1));                      // unselected row not
        Assert.NotNull(buf[0, 0].Foreground);
        Assert.Equal(buf[0, 1].Foreground, buf[0, 0].Foreground);   // the label's green survives the highlight
    }

    [Fact]
    public void IRenderableItem_UnderlineSelectionStyle_UnderlinesSelectedRow_NotOthers()
    {
        var list = new ListBox(new Spectre.Console.Text("alpha"), new Spectre.Console.Text("beta"))
        {
            SelectionStyle = SelectionStyle.Underline,
        };

        var buf = ConsoleSnapshot.Render(list, 14, 2);

        Assert.True(RowHasUnderline(buf, 0));
        Assert.False(RowHasUnderline(buf, 1));
    }

    // --- HighlightFullWidth extends the selection across the whole row ---

    // True only if EVERY cell on row y carries a background colour (the selection fills the full width).
    private static bool WholeRowHasBackground(ConsoleBuffer buf, int y)
    {
        for (var x = 0; x < buf.Size.Width; x++)
            if (buf[x, y].Background is null) return false;
        return true;
    }

    [Fact]
    public void HighlightFullWidth_FillsTheEntireSelectedRow()
    {
        var list = new ListBox("ab", "cd") { HighlightFullWidth = true };   // row 0 selected

        var buf = ConsoleSnapshot.Render(list, 12, 2);

        Assert.True(WholeRowHasBackground(buf, 0));   // the selection background spans the full row width
        Assert.False(RowHasBackground(buf, 1));       // the unselected row is untouched
    }

    [Fact]
    public void WithoutHighlightFullWidth_OnlyTheItemWidthIsHighlighted()
    {
        var list = new ListBox("ab", "cd");   // default: highlight stops at the item's own width

        var buf = ConsoleSnapshot.Render(list, 12, 2);

        Assert.NotNull(buf[0, 0].Background);                    // the item text is highlighted
        Assert.Null(buf[buf.Size.Width - 1, 0].Background);      // but not the trailing empty space
    }

    [Fact]
    public void HighlightFullWidth_FillsTheRow_ForIRenderableItems_Too()
    {
        var list = new ListBox(new Spectre.Console.Text("ab"), new Spectre.Console.Text("cd"))
        {
            HighlightFullWidth = true,
        };

        var buf = ConsoleSnapshot.Render(list, 12, 2);

        Assert.True(WholeRowHasBackground(buf, 0));
        Assert.False(RowHasBackground(buf, 1));
    }

    [Fact]
    public void IRenderableItem_CaretSelectionStyle_PrefixesSelectedRow_AndReservesGutter()
    {
        var list = new ListBox(new Spectre.Console.Text("alpha"), new Spectre.Console.Text("beta"))
        {
            SelectionStyle = SelectionStyle.Caret,
        };

        var rows = ConsoleSnapshot.ToText(list, 14, 2).TrimEnd('\n').Split('\n');

        Assert.Contains("▶", rows[0]);                          // caret on the selected IRenderable row
        Assert.DoesNotContain("▶", rows[1]);                    // not on the unselected one
        // The gutter is reserved on the unselected row too, so its label starts in the same column as the selected one.
        Assert.Equal(rows[0].IndexOf("alpha"), rows[1].IndexOf("beta"));
    }
}
