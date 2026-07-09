namespace Jumbee.Console.Tests;

using System.Linq;

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
    public void FramedCodeEditor_DoesNotRebuildWrapRowsPerConvergencePass()
    {
        // A framed CodeEditor's content height depends on its width (word wrap), which feeds the scrolling frame's
        // layout convergence — so the whole document's wrap rows can be re-measured many times for a single render.
        // Memoizing BuildVisualRows on (text, width) must keep that to a handful of computations, not one per pass.
        var editor = new CodeEditor(Language.CSharp);
        editor.Text = string.Join('\n', Enumerable.Range(0, 40).Select(i => $"var line{i} = {i} + someValue * factor;"));
        editor.WithFrame();

        var split = new SplitPanel(SplitOrientation.Horizontal, new ListBox(), editor, splitPosition: 30);

        editor.Editor.visualRowsBuilt = 0;
        ConsoleSnapshot.Render(split, 80, 12);   // narrow pane forces wrapping (activates the width->height feedback)

        // With the wrap memoized on (text, width), a whole render re-measures the document at most a couple of times
        // (a distinct width or two), not once per convergence pass — headless this dropped 9x -> 1x, and the real
        // app's doc-open blowup (hundreds of re-measures) proportionally more.
        Assert.True(editor.Editor.visualRowsBuilt < 4,
            $"BuildVisualRows ran {editor.Editor.visualRowsBuilt}x for one render — layout convergence is re-measuring the whole document.");
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
