namespace Jumbee.Console.Tests;

using System;

using Jumbee.Console;
using Jumbee.Console.Snapshot;

using Xunit;

public class CompositeControlTests
{
    #region Rendering / layout
    [Fact]
    public void CodeEditor_RendersGutterAndCode()
    {
        var ed = new CodeEditor { Text = "class Foo\n{\n}" };

        var text = ConsoleSnapshot.ToText(ed, 30, 5);

        Assert.Contains("class Foo", text);   // the editor's content
        Assert.Contains("1", text);           // gutter numbers the lines
        Assert.Contains("3", text);
    }
    #endregion

    #region Inter-child wiring
    [Fact]
    public void CodeEditor_GutterTracksEditorLineCount()
    {
        var ed = new CodeEditor();

        ed.Text = "one\ntwo\nthree\nfour";   // 4 lines -> editor.Changed -> gutter sync

        Assert.Equal(4, ed.Editor.LineCount);
        Assert.Equal(4, ed.Gutter.LineCount);
    }

    [Fact]
    public void CodeEditor_GutterIsNotFocusable()
    {
        var ed = new CodeEditor();
        Assert.False(ed.Gutter.Focusable);
    }

    [Fact]
    public void CodeEditor_Gutter_AlignsWithSoftWrappedLines()
    {
        var ed = new CodeEditor { Text = "this is a long first line that wraps\nsecond" };

        var rows = ConsoleSnapshot.ToText(ed, 24, 6).TrimEnd('\n').Split('\n');

        Assert.StartsWith(" 1 ", rows[0]);   // logical line 1
        Assert.StartsWith("   ", rows[1]);   // its wrapped continuation -> blank gutter
        Assert.StartsWith(" 2 ", rows[2]);   // logical line 2
    }

    [Fact]
    public void CodeEditor_ScrollsToKeepCaretVisible_WithGutterAndScrollbar()
    {
        var lines = new string[15];
        for (var i = 0; i < 15; i++) lines[i] = $"line {i + 1:00}";
        var ed = new CodeEditor { Text = string.Join("\n", lines) };
        ed.WithRoundedBorder();   // a frame scrolls the content-sized composite (gutter + text together)
        ed.Editor.Focus();

        ConsoleSnapshot.Render(ed, 18, 9);
        for (var i = 0; i < 5; i++)   // drive the caret + auto-scroll toward the bottom
            UI.SendInput(ed.FocusedControl!, new ConsoleKeyInfo('\0', ConsoleKey.DownArrow, false, false, false));

        var text = ConsoleSnapshot.ToText(ed, 18, 9);

        Assert.Equal(15, ed.Editor.ActualHeight);   // composite/editor sized to content, not the 1000-row fill
        Assert.Contains("15 line 15", text);        // scrolled to the bottom, gutter number aligned with its line
        Assert.DoesNotContain("line 01", text);     // the top scrolled out of view
    }
    #endregion

    #region Focus + input routing
    [Fact]
    public void FocusedControl_SurfacesFocusedChild()
    {
        var ed = new CodeEditor();
        Assert.Null(ed.FocusedControl);   // nothing focused yet

        ed.Editor.Focus();

        Assert.Same(ed.Editor, ed.FocusedControl);   // composite surfaces the focused descendant
    }

    [Fact]
    public void Input_RoutedThroughFocusedControl_ReachesEditor()
    {
        var ed = new CodeEditor();
        ed.Editor.Focus();

        // This is exactly what Layout.OnInput does: deliver to the composite's FocusedControl.
        var target = ed.FocusedControl!;
        UI.SendInput(target, new ConsoleKeyInfo('X', ConsoleKey.X, shift: false, alt: false, control: false));

        Assert.Contains("X", ed.Text);
    }

    [Fact]
    public void Input_RoutesIntoFramedComposite_ReachesEditor()
    {
        // The demo frames the composite; the parent layout then sees the ControlFrame, which must delegate focus
        // down to the composite's focused descendant. Regression guard for the "framed composite ignores keys" bug.
        var ed = new CodeEditor();
        ed.WithRoundedBorder();
        ed.Editor.Focus();

        Assert.Same(ed.FocusableControl, ed.FocusableControl.FocusedControl);   // the frame is the routing node

        // UI.SendInput delivers to ed.FocusableControl (the frame), mirroring the live input path.
        UI.SendInput(ed, new ConsoleKeyInfo('Z', ConsoleKey.Z, shift: false, alt: false, control: false));

        Assert.Contains("Z", ed.Text);
    }
    #endregion
}
