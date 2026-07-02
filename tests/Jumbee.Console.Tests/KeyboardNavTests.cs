namespace Jumbee.Console.Tests;

using System;

using Jumbee.Console;
using Jumbee.Console.Snapshot;

using Xunit;

/// <summary>Keyboard-navigation coverage for the bindings added in the nav audit: list/tree/toggle-list
/// Home/End/PageUp/PageDown, dropdown open-on-arrow, and tab-strip plain-arrow switching.</summary>
public class KeyboardNavTests
{
    private static ConsoleKeyInfo K(ConsoleKey k) => new('\0', k, false, false, false);

    #region ListBox
    [Fact]
    public void ListBox_EndSelectsLast_HomeSelectsFirst()
    {
        var list = new ListBox();
        for (var i = 0; i < 20; i++) list.AddItem($"item {i:00}");

        ConsoleSnapshot.RenderAfter(list, 16, 6, ConsoleKey.End);
        Assert.Equal(19, list.SelectedIndex);

        ConsoleSnapshot.RenderAfter(list, 16, 6, ConsoleKey.Home);
        Assert.Equal(0, list.SelectedIndex);
    }

    [Fact]
    public void ListBox_PageDownThenPageUp_MovesByAPage()
    {
        var list = new ListBox();
        for (var i = 0; i < 40; i++) list.AddItem($"item {i:00}");
        list.WithRoundedBorder();                       // framed -> page = viewport
        ConsoleSnapshot.Render(list, 16, 8);            // viewport ~6 rows

        UI.SendInput(list, K(ConsoleKey.PageDown));
        Assert.True(list.SelectedIndex > 0 && list.SelectedIndex < 20,
            $"PageDown should move down about a page (was {list.SelectedIndex})");

        var afterDown = list.SelectedIndex;
        UI.SendInput(list, K(ConsoleKey.PageUp));
        Assert.True(list.SelectedIndex < afterDown, "PageUp should move back up");
    }
    #endregion

    #region Tree
    [Fact]
    public void Tree_EndSelectsLastVisibleNode_HomeReturnsToTop()
    {
        var tree = new Tree("root");
        for (var i = 0; i < 6; i++) tree.AddNode($"node{i}");
        ConsoleSnapshot.Render(tree, 20, 10);

        UI.SendInput(tree, K(ConsoleKey.End));
        Assert.Equal("node5", tree.SelectedNode?.Text);

        UI.SendInput(tree, K(ConsoleKey.Home));
        var home = tree.SelectedNode?.Text;
        Assert.NotEqual("node5", home);   // Home moved off the last node (to the top of the visible tree)
    }
    #endregion

    #region RadioSet / SelectionList (ToggleList)
    [Fact]
    public void RadioSet_EndMovesCursorToLast_HomeToFirst()
    {
        var radio = new RadioSet("one", "two", "three", "four");

        ConsoleSnapshot.RenderAfter(radio, 20, 4, ConsoleKey.End);
        Assert.Equal(3, radio.CursorIndex);

        ConsoleSnapshot.RenderAfter(radio, 20, 4, ConsoleKey.Home);
        Assert.Equal(0, radio.CursorIndex);
    }
    #endregion

    #region Select (dropdown)
    [Fact]
    public void Select_DownArrow_OpensTheDropdown()
    {
        var select = new Select("Red", "Green", "Blue");
        var overlay = new Overlay(new Grid([1], [20], [[select]]));
        UI.Overlay = overlay;
        ConsoleSnapshot.Render(overlay, 24, 12);
        UI.SetFocus(select);

        UI.SendInput(select, K(ConsoleKey.DownArrow));

        Assert.True(overlay.IsShowing);   // the dropdown opened (the standard combobox open key)
        overlay.Hide();                   // don't leak an open popup into the next test (shared static UI.Overlay)
    }
    #endregion

    #region TabPanel plain-arrow tab switching
    [Fact]
    public void TabPanel_PlainArrows_SwitchTabs_WhenHeaderFocused()
    {
        var a = new TextLabel(TextLabelOrientation.Horizontal, "AAAA");
        var b = new TextLabel(TextLabelOrientation.Horizontal, "BBBB");
        var panel = new TabPanel(TabBarDock.Top, ("One", a), ("Two", b));
        ConsoleSnapshot.Render(panel, 24, 6);
        panel.Headers[0].Focus();

        UI.SendInput(panel, K(ConsoleKey.RightArrow));
        Assert.Equal(1, panel.SelectedIndex);
        Assert.True(panel.Headers[1].IsFocused);   // focus follows to the new header for continued arrowing

        UI.SendInput(panel, K(ConsoleKey.LeftArrow));
        Assert.Equal(0, panel.SelectedIndex);
    }

    [Fact]
    public void TabPanel_PlainArrows_ReachContent_WhenContentFocused()
    {
        // With interactive content focused, plain arrows belong to it — they must NOT switch tabs.
        var list = new ListBox("a", "b", "c");
        var other = new TextLabel(TextLabelOrientation.Horizontal, "OTHER");
        var panel = new TabPanel(TabBarDock.Top, ("List", list), ("Other", other));
        ConsoleSnapshot.Render(panel, 24, 8);
        list.Focus();

        UI.SendInput(panel, K(ConsoleKey.DownArrow));

        Assert.Equal(0, panel.SelectedIndex);   // tab did not switch...
        Assert.Equal(1, list.SelectedIndex);    // ...the list moved its selection instead
    }
    #endregion
}
