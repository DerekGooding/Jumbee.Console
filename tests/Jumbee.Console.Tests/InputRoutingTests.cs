namespace Jumbee.Console.Tests;

using System;

using Jumbee.Console;

using Xunit;

public class InputRoutingTests
{
    [Fact]
    public void Focus_IsExclusive_ClearsThePreviouslyFocusedControl()
    {
        // Regression: Focus() used to set IsFocused directly, which left the previous control focused too — so
        // keyboard routing would deliver a key to BOTH. Focus() must move focus exclusively (like click-to-focus).
        var a = new Button("A");
        var b = new Button("B");

        a.Focus();
        Assert.True(a.IsFocused);

        b.Focus();
        Assert.True(b.IsFocused);
        Assert.False(a.IsFocused);   // focusing b cleared a
    }

    [Fact]
    public void Layout_RoutesKey_ToTheFocusedCell_Only()
    {
        var b1 = new Button("1");
        var b2 = new Button("2");
        int a1 = 0, a2 = 0;
        b1.Activated += (_, _) => a1++;
        b2.Activated += (_, _) => a2++;

        var grid = new Grid([1, 1], [10], [[b1], [b2]]);
        b2.Focus();

        // Enter activates the focused button; the unfocused one must not see it.
        UI.SendInput(grid, new ConsoleKeyInfo('\0', ConsoleKey.Enter, false, false, false));

        Assert.Equal(0, a1);
        Assert.Equal(1, a2);
    }
}
