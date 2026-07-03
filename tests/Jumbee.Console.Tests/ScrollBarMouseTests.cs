namespace Jumbee.Console.Tests;

using System;

using ConsoleGUI;
using ConsoleGUI.Input;
using ConsoleGUI.Space;

using Jumbee.Console;
using Jumbee.Console.Snapshot;

using Xunit;

public class ScrollBarMouseTests
{
    // A framed, content-taller-than-viewport editor: rendered at 18x9 the frame's viewport is 7 rows (border 1 each
    // side) over 15 content rows, so the scrollbar is active. controlTop = 1 (top border).
    private static CodeEditor TallFramedEditor()
    {
        var lines = new string[15];
        for (var i = 0; i < 15; i++) lines[i] = $"line {i + 1:00}";
        var ed = new CodeEditor { Text = string.Join("\n", lines) };
        ed.WithRoundedBorder();
        ConsoleSnapshot.Render(ed, 18, 9);
        return ed;
    }

    private const int ControlTop = 1;    // top border
    private const int ScrollCol = 16;    // right edge (18 - 1 - 1); the handler ignores X, this is just realistic

    [Fact]
    public void Wheel_OverScrollbar_Scrolls()
    {
        var ed = TallFramedEditor();
        Assert.Equal(0, ed.Frame!.Top);

        ((IMouseWheelListener)ed.Frame!).OnMouseWheel(new Position(ScrollCol, 3), 3);

        Assert.True(ed.Frame!.Top > 0);
    }

    [Fact]
    public void ClickBelowThumb_PagesDown()
    {
        var ed = TallFramedEditor();

        // Bottom of the bar is below the thumb when scrolled to the top -> pages (or steps) down.
        ((IMouseListener)ed.Frame!).OnMouseDown(new Position(ScrollCol, ControlTop + 6));

        Assert.True(ed.Frame!.Top > 0);
    }

    [Fact]
    public void ClickAboveThumb_PagesUp()
    {
        var ed = TallFramedEditor();
        ed.Frame!.Top = 100;                        // scroll to the bottom (clamped to max)
        var atBottom = ed.Frame!.Top;
        Assert.True(atBottom > 0);

        // Top of the bar is above the thumb when scrolled to the bottom -> pages (or steps) up.
        ((IMouseListener)ed.Frame!).OnMouseDown(new Position(ScrollCol, ControlTop));

        Assert.True(ed.Frame!.Top < atBottom);
    }

    [Fact]
    public void DragThumb_Scrolls_AndCapturesThenReleases()
    {
        var ed = TallFramedEditor();
        var frame = ed.Frame!;
        var ml = (IMouseListener)frame;
        try
        {
            Assert.Equal(0, frame.Top);

            ml.OnMouseDown(new Position(ScrollCol, ControlTop + 1));   // grab the thumb (near the top of the track)
            Assert.Same(frame, ConsoleManager.MouseCapture);           // dragging captures the mouse

            ml.OnMouseMove(new Position(ScrollCol, ControlTop + 5));    // drag down
            Assert.True(frame.Top > 0);                                // scrolled down

            ml.OnMouseUp(new Position(ScrollCol, ControlTop + 5));
            Assert.Null(ConsoleManager.MouseCapture);                  // released on button-up
        }
        finally
        {
            ConsoleManager.ReleaseMouseCapture();
        }
    }
}
