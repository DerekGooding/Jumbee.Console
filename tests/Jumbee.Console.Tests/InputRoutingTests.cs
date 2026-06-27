namespace Jumbee.Console.Tests;

using System;

using ConsoleGUI.Input;

using Jumbee.Console;
using Jumbee.Console.Snapshot;

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

    [Fact]
    public void FocusNext_ThenFocusPrevious_RoundTripsToTheSameControl()
    {
        // Tab/Shift+Tab are exact inverses, so next-then-previous returns to the start — deterministic regardless of
        // whatever other controls are registered in the process (the global focus ring).
        var a = new Button("A");
        var b = new Button("B");
        ConsoleSnapshot.Render(a, 10, 1);   // give them a layout so they're in the ring (HasLayout)
        ConsoleSnapshot.Render(b, 10, 1);

        a.Focus();
        UI.FocusNext();
        var moved = UI.Focused;
        UI.FocusPrevious();

        Assert.NotSame(a, moved);        // FocusNext actually advanced to another control
        Assert.Same(a, UI.Focused);      // FocusPrevious came back
    }

    [Fact]
    public void FocusTraversal_OnlyVisitsInteractiveControls()
    {
        // A non-interactive control (a label: HandlesInput == false) must never be a tab stop.
        var button = new Button("go");
        var label = new TextLabel(TextLabelOrientation.Horizontal, "label");
        ConsoleSnapshot.Render(button, 10, 1);
        ConsoleSnapshot.Render(label, 10, 1);

        button.Focus();
        for (var i = 0; i < 6; i++)
        {
            UI.FocusNext();
            Assert.IsNotType<TextLabel>(UI.Focused);                  // never lands on the label
            Assert.True(UI.Focused is Control { HandlesInput: true }); // always an interactive control
        }
    }

    [Fact]
    public void CtrlN_IsBoundToFocusTraversal_ByDefault()
    {
        var a = new Button("A");
        var b = new Button("B");
        ConsoleSnapshot.Render(a, 10, 1);
        ConsoleSnapshot.Render(b, 10, 1);
        a.Focus();

        // Ctrl+N is a default global hotkey -> FocusNext (the live hotkey path).
        var e = new InputEvent(UI.HotKeys.CtrlN);
        new UI.GlobalInputListener().OnInput(e);

        Assert.True(e.Handled);          // consumed as a global hotkey (doesn't reach the focused control)
        Assert.NotSame(a, UI.Focused);   // focus advanced
    }
}
