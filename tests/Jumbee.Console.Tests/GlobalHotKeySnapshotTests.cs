namespace Jumbee.Console.Tests;

using System;

using Jumbee.Console.Snapshot;

using Xunit;

/// <summary>Global hotkeys registered with <see cref="UI.RegisterHotKey"/> can be exercised headlessly through the
/// snapshot input path when <c>routeGlobal: true</c> is passed — mirroring the live <c>UI.OnInput</c> dispatch, so a
/// TUI app's keybindings can be validated in a test without a real terminal.</summary>
public class GlobalHotKeySnapshotTests
{
    [Fact]
    public void GlobalHotKey_FiresThroughSnapshotInput_OnlyWhenRouteGlobal()
    {
        // Build the key the same way it is registered so the ConsoleKeyInfo compares equal in GlobalHotKeys.
        var key = new ConsoleKeyInfo('r', ConsoleKey.R, shift: false, alt: false, control: false);
        var fired = 0;
        UI.RegisterHotKey(key, () => fired++);
        try
        {
            var target = new ListBox("alpha", "beta");   // any focusable control to receive/render

            // Default (routeGlobal: false): the key goes straight to the control; the global hotkey never fires.
            ConsoleSnapshot.RenderAfter(target, 20, 6, new[] { key });
            Assert.Equal(0, fired);

            // routeGlobal: true runs the global hotkey dispatch first.
            ConsoleSnapshot.RenderAfter(target, 20, 6, new[] { key }, routeGlobal: true);
            Assert.Equal(1, fired);
        }
        finally
        {
            UI.UnregisterHotKey(key);
        }
    }

    [Fact]
    public void GlobalHotKey_MutationIsVisibleInSnapshot()
    {
        var key = new ConsoleKeyInfo('a', ConsoleKey.A, shift: false, alt: false, control: false);
        var list = new ListBox("one", "two");
        UI.RegisterHotKey(key, () => list.AddItem("MARKED"));   // the kind of state change a keybinding drives
        try
        {
            Assert.DoesNotContain("MARKED", ConsoleSnapshot.ToText(list, 20, 8));

            var after = ConsoleSnapshot.ToTextAfter(list, 20, 8, new[] { key }, routeGlobal: true);
            Assert.Contains("MARKED", after);
        }
        finally
        {
            UI.UnregisterHotKey(key);
        }
    }
}
