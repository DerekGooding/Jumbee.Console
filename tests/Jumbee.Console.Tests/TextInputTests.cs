namespace Jumbee.Console.Tests;

using System;
using System.Linq;

using ConsoleGUI;

using Jumbee.Console.Snapshot;

using Xunit;

/// <summary>Headless tests for <see cref="TextInput"/>: rendering (text, placeholder, password, scroll), the
/// native cursor, editing/navigation via routed key input, selection, and the Changed/Submitted events.</summary>
public class TextInputTests
{
    private static ConsoleKeyInfo Type(char c) => new(c, (ConsoleKey)char.ToUpperInvariant(c), false, false, false);
    private static ConsoleKeyInfo K(ConsoleKey k, bool shift = false, bool control = false) => new('\0', k, shift, false, control);

    private static (int x, int y)? FindCursor(ConsoleBuffer buf)
    {
        for (var y = 0; y < buf.Size.Height; y++)
            for (var x = 0; x < buf.Size.Width; x++)
                if (buf[x, y].Character.IsCursor) return (x, y);
        return null;
    }

    private static void Send(TextInput t, params ConsoleKeyInfo[] keys)
    {
        foreach (var k in keys) UI.SendInput(t, k);
    }

    [Fact]
    public void Renders_Text() => Assert.Contains("hello", ConsoleSnapshot.ToText(new TextInput("hello"), 20, 1));

    [Fact]
    public void Placeholder_ShownWhenEmpty()
    {
        var t = new TextInput(text: "", placeholder: "Value");
        Assert.Contains("Value", ConsoleSnapshot.ToText(t, 20, 1));
    }

    [Fact]
    public void Placeholder_HiddenOnceTextEntered()
    {
        var t = new TextInput(text: "x", placeholder: "Value");
        Assert.DoesNotContain("Value", ConsoleSnapshot.ToText(t, 20, 1));
    }

    [Fact]
    public void Typing_InsertsText_AndRaisesChanged()
    {
        var t = new TextInput();
        var changes = 0;
        t.Changed += (_, _) => changes++;
        ConsoleSnapshot.Render(t, 20, 1);   // size first

        Send(t, Type('h'), Type('i'));

        Assert.Equal("hi", t.Text);
        Assert.Equal(2, changes);
        Assert.Contains("hi", ConsoleSnapshot.ToText(t, 20, 1));
    }

    [Fact]
    public void Backspace_DeletesBeforeCaret()
    {
        var t = new TextInput("ab");   // caret at end
        ConsoleSnapshot.Render(t, 20, 1);
        Send(t, K(ConsoleKey.Backspace));
        Assert.Equal("a", t.Text);
    }

    [Fact]
    public void HomeThenDelete_RemovesFirstChar()
    {
        var t = new TextInput("ab");
        ConsoleSnapshot.Render(t, 20, 1);
        Send(t, K(ConsoleKey.Home), K(ConsoleKey.Delete));
        Assert.Equal("b", t.Text);
    }

    [Fact]
    public void Enter_RaisesSubmitted()
    {
        var t = new TextInput("payload");
        ConsoleSnapshot.Render(t, 20, 1);
        var submitted = false;
        t.Submitted += (_, _) => submitted = true;
        Send(t, K(ConsoleKey.Enter));
        Assert.True(submitted);
    }

    [Fact]
    public void FocusedInput_DrawsCursor_UnfocusedDoesNot()
    {
        var t = new TextInput("hi");
        Assert.Null(FindCursor(ConsoleSnapshot.Render(t, 20, 1)));   // not focused
        t.IsFocused = true;
        Assert.NotNull(FindCursor(ConsoleSnapshot.Render(t, 20, 1)));
    }

    [Fact]
    public void Password_MasksTheText()
    {
        var t = new TextInput("secret") { PasswordChar = '*' };
        var text = ConsoleSnapshot.ToText(t, 20, 1);
        Assert.Contains("******", text);
        Assert.DoesNotContain("secret", text);
        Assert.Equal("secret", t.Text);   // real value preserved
    }

    [Fact]
    public void TextThatExactlyFits_IsNotScrolled()   // regression: accepting a word the width of the field hid its first char
    {
        var t = new TextInput("Authorization");        // 13 chars, caret at end
        Assert.Contains("Authorization", ConsoleSnapshot.ToText(t, 13, 1));   // field exactly as wide -> whole word shown
    }

    [Fact]
    public void LongText_ScrollsToKeepCaretVisible()
    {
        var t = new TextInput(new string('a', 30) + "END");   // caret at the end
        var text = ConsoleSnapshot.ToText(t, 10, 1);          // narrower than the content
        Assert.Contains("END", text);                         // the tail (with the caret) is shown
    }

    [Fact]
    public void ShiftLeft_SelectsTowardStart()
    {
        var t = new TextInput("hello");   // caret at end (5)
        ConsoleSnapshot.Render(t, 20, 1);
        Send(t, K(ConsoleKey.LeftArrow, shift: true), K(ConsoleKey.LeftArrow, shift: true));
        Assert.Equal("lo", t.SelectedText);
    }

    [Fact]
    public void Typing_ReplacesSelection()
    {
        var t = new TextInput("hello");
        ConsoleSnapshot.Render(t, 20, 1);
        Send(t, K(ConsoleKey.A, control: true));   // select all
        Send(t, Type('x'));
        Assert.Equal("x", t.Text);
    }

    [Fact]
    public void Selection_IsHighlighted_InRenderedBuffer()
    {
        var t = new TextInput("hello");
        ConsoleSnapshot.Render(t, 20, 1);
        Send(t, K(ConsoleKey.A, control: true));   // select all -> every glyph carries the selection background
        var buf = ConsoleSnapshot.Render(t, 20, 1);
        Assert.NotNull(buf[0, 0].Background);   // selected cell has a background fill
    }

    [Fact]
    public void Tab_IsNotConsumed()   // leaves Tab for focus traversal
    {
        var t = new TextInput();
        ConsoleSnapshot.Render(t, 20, 1);
        var evt = new UI.InputEventArgs(new ConsoleGUI.Input.InputEvent(K(ConsoleKey.Tab)));
        ((Control)t).OnInput(evt);
        Assert.False(evt.InputEvent!.Handled);
        Assert.Equal("", t.Text);
    }
}
