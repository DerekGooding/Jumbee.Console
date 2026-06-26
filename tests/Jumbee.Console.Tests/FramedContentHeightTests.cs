namespace Jumbee.Console.Tests;

using Jumbee.Console;
using Jumbee.Console.Snapshot;

using Xunit;

/// <summary>
/// A framed content control must size to its content height (via Control.MeasureHeight) so the frame's
/// scrollbar/scroll-range are accurate — not the old ~1000-row fill that produced a tiny thumb and let the
/// wheel scroll through empty space.
/// </summary>
public class FramedContentHeightTests
{
    [Fact]
    public void FramedListBox_SizesToItemCount_NotMaxFill()
    {
        var list = new ListBox();
        for (var i = 0; i < 25; i++) list.AddItem($"Item {i}");   // taller than the viewport
        list.WithFrame();

        ConsoleSnapshot.Render(list, 24, 10);

        Assert.Equal(25, list.ActualHeight);     // sized to its 25 items, not the 1000-row clamp
    }

    [Fact]
    public void FramedListBox_DoesNotOverScrollPastContent()
    {
        var list = new ListBox();
        for (var i = 0; i < 40; i++) list.AddItem($"Item {i}");
        list.WithFrame();

        ConsoleSnapshot.Render(list, 24, 10);
        var frame = (ControlFrame)list.FocusableControl;

        frame.Top = 1000;                        // try to scroll way past the content
        ConsoleSnapshot.Render(list, 24, 10);

        var viewport = frame.ViewportSize.Height;
        Assert.Equal(40, list.ActualHeight);                     // content-sized
        Assert.True(frame.Top <= 40 - viewport,                  // clamped to content, not 1000
            $"Top {frame.Top} should clamp to content (40) - viewport ({viewport})");
    }

    [Fact]
    public void UnframedListBox_StillFillsItsCell()
    {
        // A finite parent (here the snapshot's fixed size) must still fill, exactly as before — MeasureHeight only
        // applies under an unbounded (scrolling) parent.
        var list = new ListBox();
        list.AddItem("only one");

        ConsoleSnapshot.Render(list, 24, 8);

        Assert.Equal(8, list.ActualHeight);   // fills the 8-row cell, not shrunk to 1 item
    }
}
