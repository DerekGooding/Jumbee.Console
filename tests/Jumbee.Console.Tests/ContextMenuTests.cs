namespace Jumbee.Console.Tests;

using ConsoleGUI.Input;

using Jumbee.Console.Snapshot;

using Xunit;

/// <summary>Headless tests for <see cref="ContextMenu"/>: rendering, keyboard navigation (skipping separators and
/// disabled items), activation (action + event + close), and mouse-click activation.</summary>
public class ContextMenuTests
{
    private static void SendKey(Control c, ConsoleKey k)
        => ((Control)c).OnInput(new UI.InputEventArgs(new InputEvent(new ConsoleKeyInfo('\0', k, false, false, false))));

    // Shows a menu in a fresh overlay (bottom is a trivial label) and returns the overlay.
    private static Overlay ShowMenu(ContextMenu menu)
    {
        var overlay = new Overlay(new Grid([1], [20], [[new TextLabel(TextLabelOrientation.Horizontal, "bg", Color.White)]]));
        menu.Show(overlay, 0, 0);
        return overlay;
    }

    [Fact]
    public void Renders_Items()
    {
        var menu = new ContextMenu([new MenuItem("New"), new MenuItem("Open"), new MenuItem("Save")]);
        var overlay = ShowMenu(menu);
        var text = ConsoleSnapshot.ToText(overlay, 24, 10);
        Assert.Contains("New", text);
        Assert.Contains("Open", text);
        Assert.Contains("Save", text);
    }

    [Fact]
    public void Renders_Shortcut()
    {
        var menu = new ContextMenu([new MenuItem("Save") { Shortcut = "Ctrl+S" }]);
        var overlay = ShowMenu(menu);
        Assert.Contains("Ctrl+S", ConsoleSnapshot.ToText(overlay, 24, 10));
    }

    [Fact]
    public void Enter_ActivatesHighlighted_RunsAction_AndCloses()
    {
        var ran = false;
        MenuItem? activated = null;
        var menu = new ContextMenu([new MenuItem("New", () => ran = true), new MenuItem("Open")]);
        menu.ItemActivated += (_, it) => activated = it;
        var overlay = ShowMenu(menu);
        Assert.True(overlay.IsShowing);

        SendKey(menu, ConsoleKey.Enter);   // first item is highlighted by default

        Assert.True(ran);
        Assert.Equal("New", activated?.Text);
        Assert.False(overlay.IsShowing);   // menu closed on activation
    }

    [Fact]
    public void Down_ThenEnter_ActivatesSecondItem()
    {
        MenuItem? activated = null;
        var menu = new ContextMenu([new MenuItem("New"), new MenuItem("Open")]);
        menu.ItemActivated += (_, it) => activated = it;
        ShowMenu(menu);

        SendKey(menu, ConsoleKey.DownArrow);
        SendKey(menu, ConsoleKey.Enter);

        Assert.Equal("Open", activated?.Text);
    }

    [Fact]
    public void Down_SkipsSeparatorAndDisabledItems()
    {
        MenuItem? activated = null;
        var menu = new ContextMenu([
            new MenuItem("New"),
            MenuItem.Separator,
            new MenuItem("Disabled") { Enabled = false },
            new MenuItem("Save"),
        ]);
        menu.ItemActivated += (_, it) => activated = it;
        ShowMenu(menu);

        SendKey(menu, ConsoleKey.DownArrow);   // from "New" -> skips separator + disabled -> "Save"
        SendKey(menu, ConsoleKey.Enter);

        Assert.Equal("Save", activated?.Text);
    }

    [Fact]
    public void Closed_FiresOnActivation()
    {
        var closed = false;
        var menu = new ContextMenu([new MenuItem("New")]);
        menu.Closed += (_, _) => closed = true;
        ShowMenu(menu);

        SendKey(menu, ConsoleKey.Enter);

        Assert.True(closed);
    }

    [Fact]
    public void Click_ActivatesRow()
    {
        MenuItem? activated = null;
        var menu = new ContextMenu([new MenuItem("New"), new MenuItem("Open")]);
        menu.ItemActivated += (_, it) => activated = it;
        var overlay = ShowMenu(menu);
        ConsoleSnapshot.Render(overlay, 24, 10);   // size the menu so its rows have coordinates

        var ml = (ConsoleGUI.Input.IMouseListener)menu;
        ml.OnMouseDown(new ConsoleGUI.Space.Position(0, 1));   // second row
        ml.OnMouseUp(new ConsoleGUI.Space.Position(0, 1));

        Assert.Equal("Open", activated?.Text);
    }
}
