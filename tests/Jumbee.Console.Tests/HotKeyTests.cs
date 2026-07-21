namespace Jumbee.Console.Tests;

using System;
using System.Linq;

using ConsoleGUI.Input;

using Jumbee.Console;

using Xunit;

public class HotKeyTests
{
    // A distinctive key that no default hotkey uses, so registering/unregistering it can't disturb other tests.
    private static readonly ConsoleKeyInfo TestKey = new('\0', ConsoleKey.F12, shift: false, alt: false, control: true);

    [Fact]
    public void RegisterHotKey_InvokesActionAndMarksHandled()
    {
        var fired = false;
        UI.RegisterHotKey(TestKey, () => fired = true);
        try
        {
            var e = new InputEvent(TestKey);
            new UI.GlobalInputListener().OnInput(e);

            Assert.True(fired);
            Assert.True(e.Handled);   // a consumed hotkey doesn't fall through to the focused control
        }
        finally
        {
            UI.UnregisterHotKey(TestKey);
        }
    }

    [Fact]
    public void UnregisterHotKey_StopsHandling()
    {
        var fired = false;
        UI.RegisterHotKey(TestKey, () => fired = true);
        UI.UnregisterHotKey(TestKey);

        var e = new InputEvent(TestKey);
        new UI.GlobalInputListener().OnInput(e);

        Assert.False(fired);
        Assert.False(e.Handled);
    }

    [Fact]
    public void HotKeys_EscapeAndTab_MatchDecodedKeys()
    {
        // The dict keys on ConsoleKeyInfo (KeyChar included), so the HotKeys constants must equal exactly what
        // the input decoder emits for a plain Escape / Tab press.
        Assert.Equal(new ConsoleKeyInfo('\x1b', ConsoleKey.Escape, false, false, false), UI.HotKeys.Escape);
        Assert.Equal(new ConsoleKeyInfo('\t', ConsoleKey.Tab, false, false, false), UI.HotKeys.Tab);
    }

    [Theory]
    [InlineData('z')]   // bare letter
    [InlineData('Q')]   // uppercase -> Shift
    [InlineData('5')]   // digit
    [InlineData('/')]   // punctuation -> ConsoleKey 0, char only
    [InlineData(' ')]   // space
    public void HotKeys_Char_EqualsWhatTheDecoderEmits(char c)
    {
        // A hotkey registered with Char(c) must equal exactly the ConsoleKeyInfo the input decoder produces for
        // that keypress — otherwise the dict lookup in GlobalInputListener would never match a real press.
        var decoded = ((KeyInputEvent)new AnsiInputDecoder().Feed(c.ToString()).Single()).ToConsoleKeyInfo();
        Assert.Equal(decoded, UI.HotKeys.Char(c));
    }

    [Fact]
    public void HotKeys_Char_RegistersABareLetterHotkey_ThatFiresOnADecodedKeypress()
    {
        var key = UI.HotKeys.Char('z');
        var fired = false;
        UI.RegisterHotKey(key, () => fired = true);
        try
        {
            // Feed a real 'z' through the decoder, then dispatch it the way the live input path does.
            var decoded = ((KeyInputEvent)new AnsiInputDecoder().Feed("z").Single()).ToConsoleKeyInfo();
            var e = new InputEvent(decoded);
            new UI.GlobalInputListener().OnInput(e);

            Assert.True(fired);
            Assert.True(e.Handled);
        }
        finally
        {
            UI.UnregisterHotKey(key);
        }
    }
}
