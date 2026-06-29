namespace Jumbee.Console.Tests;

using ConsoleGUI.Input;
using ConsoleGUI.Space;

using Jumbee.Console.Snapshot;

using Xunit;

/// <summary>Headless tests for <see cref="DataTable"/>: rendering, selection + navigation, activation, click-select,
/// scrolling over a window, and the selected-row highlight.</summary>
public class DataTableTests
{
    private static void SendKey(Control c, ConsoleKey k)
        => ((Control)c).OnInput(new UI.InputEventArgs(new InputEvent(new ConsoleKeyInfo('\0', k, false, false, false))));

    private static DataTable Make(int rows = 3)
    {
        var dt = new DataTable("Key", "Value");
        for (var i = 0; i < rows; i++) dt.AddRow($"k{i}", $"v{i}");
        return dt;
    }

    [Fact]
    public void Renders_ColumnsAndRows()
    {
        var text = ConsoleSnapshot.ToText(Make(), 28, 10);
        Assert.Contains("Key", text);
        Assert.Contains("Value", text);
        Assert.Contains("k0", text);
        Assert.Contains("v2", text);
    }

    [Fact]
    public void DefaultSelection_IsFirstRow()
    {
        var dt = Make();
        ConsoleSnapshot.Render(dt, 28, 10);
        Assert.Equal(0, dt.SelectedIndex);
    }

    [Fact]
    public void Down_MovesSelection_AndRaisesEvent()
    {
        var dt = Make();
        var seen = -1;
        dt.SelectionChanged += (_, i) => seen = i;
        ConsoleSnapshot.Render(dt, 28, 10);

        SendKey(dt, ConsoleKey.DownArrow);

        Assert.Equal(1, dt.SelectedIndex);
        Assert.Equal(1, seen);
    }

    [Fact]
    public void Enter_ActivatesSelectedRow()
    {
        var dt = Make();
        var activated = -1;
        dt.RowActivated += (_, i) => activated = i;
        ConsoleSnapshot.Render(dt, 28, 10);

        SendKey(dt, ConsoleKey.DownArrow);
        SendKey(dt, ConsoleKey.Enter);

        Assert.Equal(1, activated);
    }

    [Fact]
    public void End_SelectsLastRow()
    {
        var dt = Make(5);
        ConsoleSnapshot.Render(dt, 28, 10);
        SendKey(dt, ConsoleKey.End);
        Assert.Equal(4, dt.SelectedIndex);
    }

    [Fact]
    public void Empty_HasNoSelection()
    {
        var dt = new DataTable("Key", "Value");
        ConsoleSnapshot.Render(dt, 28, 10);
        Assert.Equal(-1, dt.SelectedIndex);
        Assert.Null(dt.SelectedRow);
    }

    [Fact]
    public void Scrolls_ToKeepSelectionVisible()
    {
        var dt = Make(30);
        ConsoleSnapshot.Render(dt, 28, 8);   // only a few rows fit
        SendKey(dt, ConsoleKey.End);          // jump to the last row

        var text = ConsoleSnapshot.ToText(dt, 28, 8);
        Assert.Contains("k29", text);          // the last row scrolled into view
        Assert.DoesNotContain("k0", text);     // the first row scrolled off
    }

    [Fact]
    public void SelectedRow_IsHighlighted_InBuffer()
    {
        var dt = Make(3);
        var buf = ConsoleSnapshot.Render(dt, 28, 10);

        // Find a line whose interior cells carry a background fill (the selected row); plain cells have none.
        var highlighted = Enumerable.Range(0, buf.Size.Height)
            .Any(y => buf[2, y].Background is not null);
        Assert.True(highlighted, "the selected row should be highlighted with a background");
    }

    [Fact]
    public void Click_SelectsRowUnderPointer()
    {
        var dt = Make(5);
        ConsoleSnapshot.Render(dt, 28, 12);

        // Row 0 renders just below the header chrome; clicking one line lower selects row 1.
        var firstRowLine = FindRowLine(dt, "k0", 28, 12);
        var ml = (IMouseListener)dt;
        ml.OnMouseDown(new Position(2, firstRowLine + 1));
        ml.OnMouseUp(new Position(2, firstRowLine + 1));

        Assert.Equal(1, dt.SelectedIndex);
    }

    // Finds the buffer line that contains the given cell text.
    private static int FindRowLine(DataTable dt, string cell, int w, int h)
    {
        var buf = ConsoleSnapshot.Render(dt, w, h);
        for (var y = 0; y < buf.Size.Height; y++)
        {
            var line = string.Concat(Enumerable.Range(0, buf.Size.Width).Select(x => buf[x, y].Content ?? ' '));
            if (line.Contains(cell)) return y;
        }
        return -1;
    }
}
