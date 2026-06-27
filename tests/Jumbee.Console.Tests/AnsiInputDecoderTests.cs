namespace Jumbee.Console.Tests;

using System;
using System.Linq;

using Jumbee.Console;

using Xunit;

public class AnsiInputDecoderTests
{
    private static TerminalInputEvent Single(string input)
    {
        var events = new AnsiInputDecoder().Feed(input);
        return Assert.Single(events);
    }

    [Fact]
    public void PrintableText_ProducesKeyEvents()
    {
        var events = new AnsiInputDecoder().Feed("Hi");
        Assert.Collection(events,
            e => Assert.Equal('H', ((KeyInputEvent)e).KeyChar),
            e => Assert.Equal('i', ((KeyInputEvent)e).KeyChar));

        var h = (KeyInputEvent)events[0];
        Assert.Equal(ConsoleKey.H, h.Key);
        Assert.True(h.Modifiers.HasFlag(TerminalModifiers.Shift)); // uppercase
    }

    [Fact]
    public void ControlChar_DecodesAsCtrlLetter()
    {
        var e = (KeyInputEvent)Single(""); // Ctrl+C
        Assert.Equal(ConsoleKey.C, e.Key);
        Assert.True(e.Modifiers.HasFlag(TerminalModifiers.Control));
    }

    [Theory]
    [InlineData("[A", ConsoleKey.UpArrow)]
    [InlineData("[B", ConsoleKey.DownArrow)]
    [InlineData("[C", ConsoleKey.RightArrow)]
    [InlineData("[D", ConsoleKey.LeftArrow)]
    [InlineData("[H", ConsoleKey.Home)]
    [InlineData("[F", ConsoleKey.End)]
    [InlineData("[3~", ConsoleKey.Delete)]
    [InlineData("[5~", ConsoleKey.PageUp)]
    [InlineData("[6~", ConsoleKey.PageDown)]
    [InlineData("OP", ConsoleKey.F1)]
    [InlineData("[15~", ConsoleKey.F5)]
    public void NavigationAndFunctionKeys(string input, ConsoleKey expected)
    {
        var e = (KeyInputEvent)Single(input);
        Assert.Equal(expected, e.Key);
    }

    [Fact]
    public void ModifiedKey_DecodesModifiers()
    {
        var ctrlRight = (KeyInputEvent)Single("[1;5C"); // Ctrl+Right
        Assert.Equal(ConsoleKey.RightArrow, ctrlRight.Key);
        Assert.Equal(TerminalModifiers.Control, ctrlRight.Modifiers);

        var shiftUp = (KeyInputEvent)Single("[1;2A"); // Shift+Up
        Assert.Equal(ConsoleKey.UpArrow, shiftUp.Key);
        Assert.Equal(TerminalModifiers.Shift, shiftUp.Modifiers);
    }

    [Fact]
    public void ShiftTab_DecodesAsTabWithShift()
    {
        var e = (KeyInputEvent)Single("[Z");
        Assert.Equal(ConsoleKey.Tab, e.Key);
        Assert.True(e.Modifiers.HasFlag(TerminalModifiers.Shift));
        // The focus-traversal hotkey must equal exactly what a back-tab decodes to (the dict keys on ConsoleKeyInfo).
        Assert.Equal(UI.HotKeys.ShiftTab, e.ToConsoleKeyInfo());
    }

    [Fact]
    public void SgrMouse_LeftPressAndRelease()
    {
        var down = (MouseInputEvent)Single("[<0;15;8M");
        Assert.Equal(14, down.X);  // 1-based -> 0-based
        Assert.Equal(7, down.Y);
        Assert.Equal(TerminalMouseButton.Left, down.Button);
        Assert.Equal(TerminalMouseKind.Down, down.Kind);

        var up = (MouseInputEvent)Single("[<0;15;8m");
        Assert.Equal(TerminalMouseKind.Up, up.Kind);
    }

    [Fact]
    public void SgrMouse_WheelAndDragAndModifiers()
    {
        var wheelUp = (MouseInputEvent)Single("[<64;3;4M");
        Assert.Equal(TerminalMouseButton.WheelUp, wheelUp.Button);
        Assert.Equal(TerminalMouseKind.Wheel, wheelUp.Kind);

        var wheelDown = (MouseInputEvent)Single("[<65;3;4M");
        Assert.Equal(TerminalMouseButton.WheelDown, wheelDown.Button);

        // button 0 + motion bit (32) = left-drag
        var drag = (MouseInputEvent)Single("[<32;5;5M");
        Assert.Equal(TerminalMouseButton.Left, drag.Button);
        Assert.Equal(TerminalMouseKind.Drag, drag.Kind);

        // Ctrl+left-click: low button 0 + ctrl bit (16)
        var ctrlClick = (MouseInputEvent)Single("[<16;1;1M");
        Assert.Equal(TerminalMouseButton.Left, ctrlClick.Button);
        Assert.True(ctrlClick.Modifiers.HasFlag(TerminalModifiers.Control));
    }

    [Fact]
    public void BracketedPaste_DeliveredAsOneEvent_PreservingNewlines()
    {
        var paste = (PasteInputEvent)Single("[200~line1\nline2[201~");
        Assert.Equal("line1\nline2", paste.Text);
    }

    [Fact]
    public void BracketedPaste_ContentWithEscapeLikeBytesIsNotInterpreted()
    {
        // Paste body contains text that looks like a CSI sequence; it must be treated literally.
        var paste = (PasteInputEvent)Single("[200~a[Db[201~");
        Assert.Equal("a[Db", paste.Text);
    }

    [Fact]
    public void Focus_InAndOut()
    {
        Assert.True(((FocusInputEvent)Single("[I")).HasFocus);
        Assert.False(((FocusInputEvent)Single("[O")).HasFocus);
    }

    [Fact]
    public void SplitSequenceAcrossFeeds_IsBuffered()
    {
        var decoder = new AnsiInputDecoder();
        Assert.Empty(decoder.Feed(""));    // lone ESC: held
        Assert.Empty(decoder.Feed("[1;5"));      // partial CSI: held
        var events = decoder.Feed("C");          // completes Ctrl+Right
        var e = (KeyInputEvent)Assert.Single(events);
        Assert.Equal(ConsoleKey.RightArrow, e.Key);
        Assert.Equal(TerminalModifiers.Control, e.Modifiers);
    }

    [Fact]
    public void SplitPasteAcrossFeeds_IsBuffered()
    {
        var decoder = new AnsiInputDecoder();
        Assert.Empty(decoder.Feed("[200~hel"));
        Assert.Empty(decoder.Feed("lo"));
        var paste = (PasteInputEvent)Assert.Single(decoder.Feed("[201~"));
        Assert.Equal("hello", paste.Text);
    }

    [Fact]
    public void PasteFedOneCharAtATime_AccumulatesIncrementally()
    {
        // Drives the incremental paste path (and a terminator split across feeds): each char is its own Feed.
        var decoder = new AnsiInputDecoder();
        var body = "the quick brown fox\njumps over";
        var full = "[200~" + body + "[201~";
        var collected = new System.Collections.Generic.List<TerminalInputEvent>();
        foreach (var ch in full)
            collected.AddRange(decoder.Feed(ch.ToString()));

        var paste = Assert.IsType<PasteInputEvent>(Assert.Single(collected));
        Assert.Equal(body, paste.Text);
    }

    [Fact]
    public void LoneEsc_ResolvesToEscapeOnFlush()
    {
        var decoder = new AnsiInputDecoder();
        Assert.Empty(decoder.Feed(""));    // held, ambiguous
        var e = (KeyInputEvent)Assert.Single(decoder.Flush());
        Assert.Equal(ConsoleKey.Escape, e.Key);
    }

    [Fact]
    public void EscThenPrintable_IsAltKey()
    {
        var e = (KeyInputEvent)Single("a"); // Alt+a ( is fixed-length; "\x1ba" would be one hex char)
        Assert.Equal(ConsoleKey.A, e.Key);
        Assert.True(e.Modifiers.HasFlag(TerminalModifiers.Alt));
    }

    [Fact]
    public void MixedStream_KeysMousePasteInOrder()
    {
        var events = new AnsiInputDecoder().Feed("a[<0;2;3M[200~x[201~[B");
        Assert.Collection(events,
            e => Assert.Equal('a', ((KeyInputEvent)e).KeyChar),
            e => Assert.Equal(TerminalMouseKind.Down, ((MouseInputEvent)e).Kind),
            e => Assert.Equal("x", ((PasteInputEvent)e).Text),
            e => Assert.Equal(ConsoleKey.DownArrow, ((KeyInputEvent)e).Key));
    }

    [Fact]
    public void Osc_IsSwallowed_BelAndStTerminated()
    {
        // / are fixed-length escapes (no hex absorption). OSC responses produce no input events.
        Assert.Empty(new AnsiInputDecoder().Feed("]0;some title"));         // BEL-terminated
        Assert.Empty(new AnsiInputDecoder().Feed("]52;c;SGVsbG8=\\"));      // ST-terminated (OSC 52)
    }

    [Fact]
    public void Osc_SurroundedByRealEvents_DoesNotCorruptStream()
    {
        var events = new AnsiInputDecoder().Feed("a]11;rgb:00/00/00b");
        Assert.Collection(events,
            e => Assert.Equal('a', ((KeyInputEvent)e).KeyChar),
            e => Assert.Equal('b', ((KeyInputEvent)e).KeyChar));
    }

    [Fact]
    public void Osc_IncompleteIsBuffered_UntilTerminator()
    {
        var decoder = new AnsiInputDecoder();
        Assert.Empty(decoder.Feed("]52;c;SGVs"));   // no terminator yet
        Assert.Empty(decoder.Feed("bG8="));               // still no terminator
        Assert.Empty(decoder.Feed(""));             // BEL completes it -> swallowed, no event
        var e = (KeyInputEvent)Assert.Single(decoder.Feed("z"));
        Assert.Equal('z', e.KeyChar);
    }

    [Fact]
    public void InterruptedCsi_FollowedByEsc_ResyncsToNewSequence()
    {
        // A truncated CSI ("[1;5") then a fresh sequence ("[B"): the ESC must abort the first and
        // start the second, not be eaten as a final byte. ( is fixed-length to avoid hex absorption.)
        var events = new AnsiInputDecoder().Feed("[1;5[B");
        var e = (KeyInputEvent)Assert.Single(events);
        Assert.Equal(ConsoleKey.DownArrow, e.Key);
    }

    [Fact]
    public void MalformedCsi_ControlByte_IsDroppedNotEmittedAsKey()
    {
        // CAN (0x18) mid-CSI cancels it; the following 'a' is the only event.
        var events = new AnsiInputDecoder().Feed("[1a");
        var e = (KeyInputEvent)Assert.Single(events);
        Assert.Equal('a', e.KeyChar);
    }

    [Fact]
    public void KeyEvent_BridgesToConsoleKeyInfo()
    {
        var e = (KeyInputEvent)Single("[1;5C");
        var cki = e.ToConsoleKeyInfo();
        Assert.Equal(ConsoleKey.RightArrow, cki.Key);
        Assert.True((cki.Modifiers & ConsoleModifiers.Control) != 0);
    }
}
