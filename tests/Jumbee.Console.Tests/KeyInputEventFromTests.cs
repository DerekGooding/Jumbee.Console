namespace Jumbee.Console.Tests;

using System;

using Jumbee.Console;

using Xunit;

/// <summary>
/// Covers the legacy keyboard decode path: <see cref="KeyInputEvent.From(ConsoleKeyInfo)"/>, used by
/// <see cref="ConsoleInputSource"/> (the non-ANSI / Windows-classic input source) to turn a <see cref="ConsoleKeyInfo"/>
/// from <c>Console.ReadKey</c> into the unified <see cref="TerminalInputEvent"/> stream. This is the legacy analogue
/// of <c>AnsiInputDecoderTests</c>; the <c>Console.ReadKey</c> read itself can't be exercised headlessly, but the
/// pure mapping can and must be.
/// </summary>
public class KeyInputEventFromTests
{
    [Fact]
    public void PlainLetter_MapsKeyCharAndKey_NoModifiers()
    {
        var e = KeyInputEvent.From(new ConsoleKeyInfo('a', ConsoleKey.A, shift: false, alt: false, control: false));
        Assert.Equal(ConsoleKey.A, e.Key);
        Assert.Equal('a', e.KeyChar);
        Assert.Equal(TerminalModifiers.None, e.Modifiers);
    }

    [Fact]
    public void Shift_SetsShiftModifier_AndCarriesShiftedChar()
    {
        var e = KeyInputEvent.From(new ConsoleKeyInfo('A', ConsoleKey.A, shift: true, alt: false, control: false));
        Assert.Equal(TerminalModifiers.Shift, e.Modifiers);
        Assert.Equal('A', e.KeyChar);
    }

    [Fact]
    public void Control_SetsControlModifier_AndPreservesKeyChar()
    {
        // The KeyChar is passed through verbatim (a real Ctrl+C reports U+0003; the exact value doesn't matter here,
        // only that From copies it and sets the Control modifier).
        var e = KeyInputEvent.From(new ConsoleKeyInfo('c', ConsoleKey.C, shift: false, alt: false, control: true));
        Assert.Equal(TerminalModifiers.Control, e.Modifiers);
        Assert.Equal(ConsoleKey.C, e.Key);
        Assert.Equal('c', e.KeyChar);
    }

    [Fact]
    public void Alt_SetsAltModifier()
    {
        var e = KeyInputEvent.From(new ConsoleKeyInfo('f', ConsoleKey.F, shift: false, alt: true, control: false));
        Assert.Equal(TerminalModifiers.Alt, e.Modifiers);
    }

    [Fact]
    public void AllModifiers_Combine()
    {
        var e = KeyInputEvent.From(new ConsoleKeyInfo('d', ConsoleKey.Delete, shift: true, alt: true, control: true));
        Assert.Equal(TerminalModifiers.Shift | TerminalModifiers.Alt | TerminalModifiers.Control, e.Modifiers);
    }

    [Theory]
    [InlineData(ConsoleKey.Enter)]
    [InlineData(ConsoleKey.Tab)]
    [InlineData(ConsoleKey.Escape)]
    [InlineData(ConsoleKey.Backspace)]
    [InlineData(ConsoleKey.UpArrow)]
    [InlineData(ConsoleKey.DownArrow)]
    [InlineData(ConsoleKey.LeftArrow)]
    [InlineData(ConsoleKey.RightArrow)]
    [InlineData(ConsoleKey.Home)]
    [InlineData(ConsoleKey.End)]
    [InlineData(ConsoleKey.PageUp)]
    [InlineData(ConsoleKey.PageDown)]
    [InlineData(ConsoleKey.F1)]
    [InlineData(ConsoleKey.F12)]
    public void SpecialKeys_MapThroughUnchanged(ConsoleKey key)
    {
        // A real special key reports KeyChar '\0'; the space here is an arbitrary placeholder — the test only asserts
        // the Key and modifier mapping, not the char.
        var e = KeyInputEvent.From(new ConsoleKeyInfo(' ', key, shift: false, alt: false, control: false));
        Assert.Equal(key, e.Key);
        Assert.Equal(TerminalModifiers.None, e.Modifiers);
    }

    [Fact]
    public void Produces_ExpectedRecord_ByValue()
    {
        // KeyInputEvent is a record: From should be exactly the value-equal event, which downstream routing relies on.
        Assert.Equal(
            new KeyInputEvent(ConsoleKey.A, 'a', TerminalModifiers.None),
            KeyInputEvent.From(new ConsoleKeyInfo('a', ConsoleKey.A, shift: false, alt: false, control: false)));
    }

    [Fact]
    public void RoundTrips_ThroughToConsoleKeyInfo()
    {
        // From(cki).ToConsoleKeyInfo() must preserve key, char, and all three modifiers — the bridge is used in both
        // directions (legacy input in, hotkey matching out).
        var cki = new ConsoleKeyInfo('Z', ConsoleKey.Z, shift: true, alt: true, control: true);
        var back = KeyInputEvent.From(cki).ToConsoleKeyInfo();
        Assert.Equal(cki.Key, back.Key);
        Assert.Equal(cki.KeyChar, back.KeyChar);
        Assert.Equal(cki.Modifiers, back.Modifiers);
    }
}
