namespace Jumbee.Console.Tests;

using ConsoleGUI;
using ConsoleGUI.Input;
using ConsoleGUI.Space;

using Jumbee.Console;
using Jumbee.Console.Snapshot;

using Xunit;

public class DataTableScrollMouseTests
{
    // 30 rows in an 8-row view (~4 data rows fit) so the scrollbar is active.
    private static DataTable Tall()
    {
        var dt = new DataTable("Key", "Value");
        for (var i = 0; i < 30; i++) dt.AddRow($"k{i:00}", $"v{i:00}");
        return dt;
    }

    private const int W = 28, H = 8;
    private static int ScrollCol(DataTable dt) => dt.ActualWidth - 1;

    // The screen line of row 0 at scroll 0 == the top of the scrollbar (ChromeTop).
    private static int BarTop(DataTable dt)
    {
        var lines = ConsoleSnapshot.ToText(dt, W, H).Split('\n');
        for (var y = 0; y < lines.Length; y++) if (lines[y].Contains("k00")) return y;
        return -1;
    }

    [Fact]
    public void Wheel_Scrolls_WithoutChangingSelection()
    {
        var dt = Tall();
        ConsoleSnapshot.Render(dt, W, H);
        Assert.Equal(0, dt.SelectedIndex);

        ((IMouseWheelListener)dt).OnMouseWheel(new Position(ScrollCol(dt), 4), 3);

        Assert.DoesNotContain("k00", ConsoleSnapshot.ToText(dt, W, H));   // scrolled down
        Assert.Equal(0, dt.SelectedIndex);                                // selection untouched (decoupled)
    }

    [Fact]
    public void DragThumb_Scrolls_AndCapturesThenReleases_WithoutSelecting()
    {
        var dt = Tall();
        ConsoleSnapshot.Render(dt, W, H);
        var top = BarTop(dt);
        Assert.True(top >= 0);
        var ml = (IMouseListener)dt;
        try
        {
            ml.OnMouseDown(new Position(ScrollCol(dt), top));       // grab the thumb (top of the track at scroll 0)
            Assert.Same(dt, ConsoleManager.MouseCapture);           // dragging captures

            ml.OnMouseMove(new Position(ScrollCol(dt), top + 2));   // drag down
            ml.OnMouseUp(new Position(ScrollCol(dt), top + 2));
            Assert.Null(ConsoleManager.MouseCapture);               // released on up
        }
        finally
        {
            ConsoleManager.ReleaseMouseCapture();
        }

        Assert.DoesNotContain("k00", ConsoleSnapshot.ToText(dt, W, H));   // scrolled down
        Assert.Equal(0, dt.SelectedIndex);                                // the drag was not a row click
    }

    [Fact]
    public void ClickTrackBelowThumb_PagesDown_WithoutSelecting()
    {
        var dt = Tall();
        ConsoleSnapshot.Render(dt, W, H);
        var top = BarTop(dt);
        var ml = (IMouseListener)dt;

        ml.OnMouseDown(new Position(ScrollCol(dt), top + 3));   // below the thumb -> page down
        ml.OnMouseUp(new Position(ScrollCol(dt), top + 3));

        Assert.DoesNotContain("k00", ConsoleSnapshot.ToText(dt, W, H));   // scrolled down a page
        Assert.Equal(0, dt.SelectedIndex);                                // clicking the bar didn't select a row
    }
}
