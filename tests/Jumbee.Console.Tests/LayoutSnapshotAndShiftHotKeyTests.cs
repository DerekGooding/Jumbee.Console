namespace Jumbee.Console.Tests;

using System;

using Jumbee.Console.Snapshot;

using Xunit;

/// <summary>Regression coverage for two API gaps surfaced by the scope-tui port: the <c>ILayout</c> overload of
/// <see cref="ConsoleSnapshot.ToTextAfter(ILayout, int, int, IReadOnlyList{ConsoleKeyInfo}, bool)"/> (key-drive then
/// snapshot a whole multi-control layout as one unit), and <see cref="UI.HotKeys.Shift"/> (build a Shift-modified key
/// for arbitrary keys, e.g. the ×10 magnitude tier's Shift+PageUp).</summary>
public class LayoutSnapshotAndShiftHotKeyTests
{
    [Fact]
    public void ToTextAfter_Layout_CapturesWholeScreen_AndReflectsGlobalHotKeyMutation()
    {
        // A header row + a body, like the scope's header-over-plot layout.
        var header = new ListBox("HEADER");
        var body = new ListBox("one", "two");
        var root = new Grid([1, 8], [24], [[header], [body]]);

        var key = new ConsoleKeyInfo('m', ConsoleKey.M, shift: false, alt: false, control: false);
        UI.RegisterHotKey(key, () => body.AddItem("MARKED"));
        try
        {
            var after = ConsoleSnapshot.ToTextAfter(root, 24, 10, new[] { key }, routeGlobal: true);

            // Both the header and the hotkey-driven body change are visible in a single layout-level snapshot.
            Assert.Contains("HEADER", after);
            Assert.Contains("MARKED", after);
        }
        finally
        {
            UI.UnregisterHotKey(key);
        }
    }

    [Fact]
    public void HotKeys_Shift_BuildsShiftModifiedKey_AndFiresGlobalHotKey()
    {
        var key = UI.HotKeys.Shift(ConsoleKey.PageUp);
        Assert.Equal(ConsoleKey.PageUp, key.Key);
        Assert.True(key.Modifiers.HasFlag(ConsoleModifiers.Shift));
        Assert.False(key.Modifiers.HasFlag(ConsoleModifiers.Control));
        Assert.False(key.Modifiers.HasFlag(ConsoleModifiers.Alt));
        Assert.Equal('\0', key.KeyChar);                          // non-letter keys carry no char
        Assert.Equal('A', UI.HotKeys.Shift(ConsoleKey.A).KeyChar); // letters carry their uppercase char

        var fired = 0;
        UI.RegisterHotKey(key, () => fired++);
        try
        {
            var target = new ListBox("x");
            ConsoleSnapshot.RenderAfter(target, 12, 4, new[] { key }, routeGlobal: true);
            Assert.Equal(1, fired);
        }
        finally
        {
            UI.UnregisterHotKey(key);
        }
    }
}
