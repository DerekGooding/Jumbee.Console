namespace Jumbee.Console.Tests;

using ConsoleGUI.Input;

using Jumbee.Console;
using Jumbee.Console.Snapshot;

using Xunit;

/// <summary>Click-to-focus routes through a layout to the control whose composited cell is under the pointer —
/// including an <em>un-framed</em> control (a bare tree docked beside a framed editor). Guards the DockPanelTest
/// observation that clicking an un-framed control should move focus to it (the real cause there was the demo using
/// the keyboard-only default input source, not a routing bug).</summary>
public class ClickFocusTests
{
    [Fact]
    public void ClickThroughDockPanel_FocusesUnframedControl()
    {
        var editor = new TextEditor(Language.Markdown);
        editor.WithRoundedBorder().WithTitle("Editor");   // framed, fills
        var tree = new Tree("tree") { Width = 20, Height = 10 };
        tree.AddNodes("Y", "Z");
        editor.Focus();                                   // start with the editor focused
        var dock = new DockPanel(DockedControlPlacement.Left, tree, editor);

        var buf = ConsoleSnapshot.Render(dock, 60, 12);

        // A cell inside the un-framed tree region carries the tree's mouse listener (the tag rides the composited
        // cell up through the DockPanel unchanged).
        var mc = buf[2, 1].MouseListener;
        Assert.NotNull(mc);
        Assert.Same(tree, mc!.Value.MouseListener);

        // Dispatch a click exactly as ConsoleManager does (listener + relative position from the cell).
        mc.Value.MouseListener!.OnMouseDown(mc.Value.RelativePosition);
        mc.Value.MouseListener!.OnMouseUp(mc.Value.RelativePosition);

        Assert.True(tree.IsFocused);       // focus moved to the un-framed tree...
        Assert.False(editor.IsFocused);    // ...off the editor
    }
}
