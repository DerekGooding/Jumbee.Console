namespace Jumbee.Console.Tests;

using ConsoleGUI.Input;

using Jumbee.Console.Snapshot;

using Xunit;

/// <summary>Headless tests for <see cref="MenuBar"/>: title rendering, opening a menu, and relaying activation.</summary>
public class MenuBarTests
{
    private static void SendKey(Control c, ConsoleKey k)
        => ((Control)c).OnInput(new UI.InputEventArgs(new InputEvent(new ConsoleKeyInfo('\0', k, false, false, false))));

    private static (MenuBar bar, Overlay overlay) Build()
    {
        var bar = new MenuBar()
            .Add("File", new MenuItem("New"), new MenuItem("Open"), MenuItem.Separator, new MenuItem("Quit"))
            .Add("Edit", new MenuItem("Cut"), new MenuItem("Copy"), new MenuItem("Paste"));
        var overlay = new Overlay(new Grid([1], [40], [[bar]]));
        UI.Overlay = overlay;   // ambient host the drop-downs float into (headless: no UI.Start)
        return (bar, overlay);
    }

    [Fact]
    public void Renders_Titles()
    {
        var (_, overlay) = Build();
        var text = ConsoleSnapshot.ToText(overlay, 40, 12);
        Assert.Contains("File", text);
        Assert.Contains("Edit", text);
    }

    [Fact]
    public void OpenActive_ShowsMenu()
    {
        var (bar, overlay) = Build();
        ConsoleSnapshot.Render(overlay, 40, 12);   // size the bar first
        bar.OpenActive();                          // opens "File" (active index 0)

        Assert.True(overlay.IsShowing);
        var text = ConsoleSnapshot.ToText(overlay, 40, 12);
        Assert.Contains("New", text);
        Assert.Contains("Quit", text);
    }

    [Fact]
    public void RightThenOpen_OpensSecondMenu()
    {
        var (bar, overlay) = Build();
        ConsoleSnapshot.Render(overlay, 40, 12);
        SendKey(bar, ConsoleKey.RightArrow);   // move active File -> Edit
        bar.OpenActive();

        var text = ConsoleSnapshot.ToText(overlay, 40, 12);
        Assert.Contains("Copy", text);
        Assert.DoesNotContain("Quit", text);   // the File menu is not the one open
    }

    [Fact]
    public void ItemActivated_IsRelayed_FromOpenMenu()
    {
        var (bar, overlay) = Build();
        MenuItem? chosen = null;
        bar.ItemActivated += (_, it) => chosen = it;
        ConsoleSnapshot.Render(overlay, 40, 12);
        bar.OpenActive();

        var menu = (ContextMenu)overlay.Top!;
        SendKey(menu, ConsoleKey.Enter);   // activates "New"

        Assert.Equal("New", chosen?.Text);
        Assert.False(overlay.IsShowing);   // menu closed after choosing
    }

    [Fact]
    public void Menu_ClosesOnActivation_ResettingOpenState()
    {
        var (bar, overlay) = Build();
        ConsoleSnapshot.Render(overlay, 40, 12);
        bar.OpenActive();
        var menu = (ContextMenu)overlay.Top!;
        SendKey(menu, ConsoleKey.Enter);

        Assert.False(overlay.IsShowing);
    }
}
