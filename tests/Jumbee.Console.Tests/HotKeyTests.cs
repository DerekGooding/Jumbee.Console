namespace Jumbee.Console.Tests;

using System;

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
}
