namespace Jumbee.Console.Tests;

using System;

using ConsoleGUI;

using Jumbee.Console;
using Jumbee.Console.Snapshot;

using Xunit;

/// <summary>Text-selection behaviour for <see cref="TextEditor"/>: Shift+navigation extends a selection, Ctrl+A
/// selects all, editing replaces/deletes it, an unmodified move collapses it, and the highlight renders.</summary>
public class TextEditorSelectionTests
{
    private static ConsoleKeyInfo Key(ConsoleKey k, bool shift = false, bool ctrl = false) => new('\0', k, shift, false, ctrl);
    private static ConsoleKeyInfo Type(char c) => new(c, (ConsoleKey)char.ToUpperInvariant(c), false, false, false);

    #region Selecting
    [Fact]
    public void ShiftRight_ExtendsSelection()
    {
        var ed = new TextEditor { Text = "hello world" };
        ed.CaretIndex = 0;

        for (var i = 0; i < 5; i++) UI.SendInput(ed, Key(ConsoleKey.RightArrow, shift: true));

        Assert.Equal("hello", ed.SelectedText);
    }

    [Fact]
    public void ShiftDown_SelectsAcrossLines()
    {
        var ed = new TextEditor { Text = "line1\nline2" };
        ConsoleSnapshot.Render(ed, 20, 4);   // lay out so vertical nav has a real wrap width
        ed.CaretIndex = 0;

        UI.SendInput(ed, Key(ConsoleKey.DownArrow, shift: true));

        Assert.Equal("line1\n", ed.SelectedText);
    }

    [Fact]
    public void CtrlA_SelectsAll()
    {
        var ed = new TextEditor { Text = "abc\ndef" };

        UI.SendInput(ed, Key(ConsoleKey.A, ctrl: true));

        Assert.Equal("abc\ndef", ed.SelectedText);
    }

    [Fact]
    public void UnmodifiedArrow_CollapsesSelection()
    {
        var ed = new TextEditor { Text = "hello" };
        ed.CaretIndex = 0;
        for (var i = 0; i < 3; i++) UI.SendInput(ed, Key(ConsoleKey.RightArrow, shift: true));   // select "hel"
        Assert.Equal("hel", ed.SelectedText);

        UI.SendInput(ed, Key(ConsoleKey.LeftArrow));   // no shift -> collapse to the selection start

        Assert.Equal("", ed.SelectedText);
        Assert.Equal(0, ed.CaretIndex);
    }
    #endregion

    #region Editing a selection
    [Fact]
    public void Typing_ReplacesSelection()
    {
        var ed = new TextEditor { Text = "hello world" };
        ed.CaretIndex = 0;
        for (var i = 0; i < 5; i++) UI.SendInput(ed, Key(ConsoleKey.RightArrow, shift: true));   // select "hello"

        UI.SendInput(ed, Type('X'));

        Assert.Equal("X world", ed.Text);
        Assert.Equal("", ed.SelectedText);
    }

    [Fact]
    public void Backspace_DeletesSelection()
    {
        var ed = new TextEditor { Text = "hello world" };
        ed.CaretIndex = 0;
        for (var i = 0; i < 6; i++) UI.SendInput(ed, Key(ConsoleKey.RightArrow, shift: true));   // "hello "

        UI.SendInput(ed, Key(ConsoleKey.Backspace));

        Assert.Equal("world", ed.Text);
    }

    [Fact]
    public void CtrlA_ThenDelete_ClearsTheDocument()
    {
        var ed = new TextEditor { Text = "abc\ndef" };
        UI.SendInput(ed, Key(ConsoleKey.A, ctrl: true));

        UI.SendInput(ed, Key(ConsoleKey.Delete));

        Assert.Equal("", ed.Text);
    }

    [Fact]
    public void Paste_ReplacesSelection()
    {
        var ed = new TextEditor { Text = "hello world" };
        ed.CaretIndex = 0;
        for (var i = 0; i < 5; i++) UI.SendInput(ed, Key(ConsoleKey.RightArrow, shift: true));   // "hello"

        ed.OnPaste("goodbye");

        Assert.Equal("goodbye world", ed.Text);
    }
    #endregion

    #region Rendering the highlight
    [Fact]
    public void Selection_RendersTheHighlightBackground()
    {
        // The default theme selection background is (40,50,80); selected cells must carry it (over the text glyphs).
        var ed = new TextEditor { Text = "hello world" };
        ed.CaretIndex = 0;
        ed.Focus();
        ConsoleSnapshot.Render(ed, 20, 3);
        for (var i = 0; i < 5; i++) UI.SendInput(ed, Key(ConsoleKey.RightArrow, shift: true));   // select "hello"

        var buf = ConsoleSnapshot.Render(ed, 20, 3);

        Assert.True(HasSelectionBg(buf, 2, 0), "a selected cell should carry the selection background");
        Assert.False(HasSelectionBg(buf, 8, 0), "an unselected cell should not");
    }

    private static bool HasSelectionBg(ConsoleBuffer buf, int x, int y)
    {
        var b = buf[x, y].Background;
        return b is { } c && c.Red == 40 && c.Green == 50 && c.Blue == 80;
    }
    #endregion
}
