namespace Jumbee.Console.Tests;

using System;

using ConsoleGUI.Input;
using ConsoleGUI.Space;

using Jumbee.Console;
using Jumbee.Console.Snapshot;

using Xunit;

public class TabPanelTests
{
    // Two tabs with distinctive content so snapshots can tell which one is shown.
    private static TabPanel TwoTabs(out TextLabel a, out TextLabel b, TabBarDock dock = TabBarDock.Top)
    {
        a = new TextLabel(TextLabelOrientation.Horizontal, "AAAA");
        b = new TextLabel(TextLabelOrientation.Horizontal, "BBBB");
        return new TabPanel(dock, ("One", a), ("Two", b));
    }

    private static void Click(TabHeader header)
    {
        var m = (IMouseListener)header;
        m.OnMouseDown(new Position(0, 0));
        m.OnMouseUp(new Position(0, 0));
    }

    #region Rendering / selection model
    [Fact]
    public void RendersHeaders_AndShowsFirstTabByDefault()
    {
        var panel = TwoTabs(out _, out _);

        var text = ConsoleSnapshot.ToText(panel, 24, 6);

        Assert.Contains("One", text);          // both tab labels in the bar
        Assert.Contains("Two", text);
        Assert.Contains("AAAA", text);         // first tab is selected by default
        Assert.DoesNotContain("BBBB", text);   // the inactive tab's content is not shown
        Assert.Equal(0, panel.SelectedIndex);
        Assert.Equal("One", panel.ActiveTabName);
    }

    [Fact]
    public void SelectedIndex_SwapsContent_MarksActive_AndRaisesEvent()
    {
        var panel = TwoTabs(out _, out _);
        int? changed = null;
        panel.SelectionChanged += i => changed = i;

        panel.SelectedIndex = 1;

        Assert.Equal(1, changed);
        Assert.True(panel.Headers[1].IsActive);
        Assert.False(panel.Headers[0].IsActive);
        var text = ConsoleSnapshot.ToText(panel, 24, 6);
        Assert.Contains("BBBB", text);
        Assert.DoesNotContain("AAAA", text);
    }

    [Fact]
    public void SelectedIndex_IsClamped_AndNoOpDoesNotRaise()
    {
        var panel = TwoTabs(out _, out _);
        var raised = 0;
        panel.SelectionChanged += _ => raised++;

        panel.SelectedIndex = 0;     // already selected -> no event
        panel.SelectedIndex = 99;    // clamps to the last tab

        Assert.Equal(1, panel.SelectedIndex);
        Assert.Equal(1, raised);     // only the real change fired
    }
    #endregion

    #region Mouse
    [Fact]
    public void ClickingHeader_SelectsThatTab()
    {
        var panel = TwoTabs(out _, out _);
        ConsoleSnapshot.Render(panel, 24, 6);   // establish layout

        Click(panel.Headers[1]);

        Assert.Equal(1, panel.SelectedIndex);
        Assert.True(panel.Headers[1].IsActive);
        Assert.False(panel.Headers[0].IsActive);
    }
    #endregion

    #region Keyboard
    [Fact]
    public void AltLeftRight_SwitchTabs_OnHorizontalBar()
    {
        var panel = TwoTabs(out _, out _);
        ConsoleSnapshot.Render(panel, 24, 6);

        // The panel intercepts Alt+arrows in its tunnel (no header focus needed).
        UI.SendInput(panel, UI.HotKeys.AltRight);
        Assert.Equal(1, panel.SelectedIndex);

        UI.SendInput(panel, UI.HotKeys.AltLeft);
        Assert.Equal(0, panel.SelectedIndex);
    }

    [Fact]
    public void AltUpDown_SwitchTabs_OnVerticalBar()
    {
        var panel = TwoTabs(out _, out _, TabBarDock.Left);
        ConsoleSnapshot.Render(panel, 24, 6);

        UI.SendInput(panel, UI.HotKeys.AltDown);
        Assert.Equal(1, panel.SelectedIndex);

        UI.SendInput(panel, UI.HotKeys.AltUp);
        Assert.Equal(0, panel.SelectedIndex);
    }

    [Fact]
    public void PlainArrow_ReachesFocusedContent_NestedInTab()
    {
        // Regression guard for "ListBox arrows don't work in a tab": a plain arrow must route grid -> tabpanel ->
        // the focused content (the panel's Alt-arrow tunnel must NOT swallow unmodified arrows).
        var list = new ListBox();
        list.AddItem("one"); list.AddItem("two"); list.AddItem("three");
        var panel = new TabPanel(TabBarDock.Top, ("L", list), ("X", new TextLabel(TextLabelOrientation.Horizontal, "x")));
        var grid = new Grid([6], [20], [[panel]]);
        ConsoleSnapshot.Render(grid, 20, 6);
        list.Focus();
        Assert.Equal(0, list.SelectedIndex);

        UI.SendInput(grid, new ConsoleKeyInfo('\0', ConsoleKey.DownArrow, false, false, false));

        Assert.Equal(1, list.SelectedIndex);   // the arrow reached the ListBox and moved its selection
    }

    [Fact]
    public void SwitchingTab_FocusesNewContent_WhenPanelHasFocus()
    {
        var listA = new ListBox();
        var listB = new ListBox();
        listA.AddItem("a"); listB.AddItem("b");
        var panel = new TabPanel(TabBarDock.Top, ("A", listA), ("B", listB));
        ConsoleSnapshot.Render(panel, 20, 6);
        panel.Headers[0].Focus();   // focus is inside the panel (on a header)

        UI.SendInput(panel, UI.HotKeys.AltRight);

        Assert.Equal(1, panel.SelectedIndex);
        Assert.True(listB.IsFocused);    // focus followed into the newly active tab's content
        Assert.False(listA.IsFocused);
    }

    [Fact]
    public void SwitchingToNonInteractiveContent_FocusesTheHeader()
    {
        var list = new ListBox();
        var label = new TextLabel(TextLabelOrientation.Horizontal, "info");   // not interactive
        var panel = new TabPanel(TabBarDock.Top, ("List", list), ("Info", label));
        ConsoleSnapshot.Render(panel, 20, 6);
        panel.Headers[0].Focus();

        UI.SendInput(panel, UI.HotKeys.AltRight);

        Assert.Equal(1, panel.SelectedIndex);
        Assert.True(panel.Headers[1].IsFocused);   // a label can't take keys, so focus rests on its header
    }

    [Fact]
    public void ProgrammaticSelect_DoesNotStealFocus_WhenPanelNotFocused()
    {
        var outside = new Button("out");
        var panel = new TabPanel(TabBarDock.Top, ("A", new ListBox()), ("B", new ListBox()));
        ConsoleSnapshot.Render(panel, 20, 6);
        ConsoleSnapshot.Render(outside, 10, 1);
        outside.Focus();   // focus is OUTSIDE the panel

        panel.SelectedIndex = 1;

        Assert.Equal(1, panel.SelectedIndex);
        Assert.True(outside.IsFocused);   // selection change didn't steal focus
    }

    [Fact]
    public void AltArrows_SwitchTabs_WhileNestedAndContentFocused()
    {
        // The whole point of handling this in the panel's tunnel: switch tabs from a parent layout, while the
        // active tab's CONTENT (not a header) holds focus. Exercises nested-layout tunnel routing.
        var contentA = new Button("A");
        var contentB = new Button("B");
        var panel = new TabPanel(TabBarDock.Top, ("One", contentA), ("Two", contentB));
        var grid = new Grid([6], [20], [[panel]]);   // panel nested inside a grid (the root)
        ConsoleSnapshot.Render(grid, 20, 6);
        contentA.Focus();                            // focus is in tab 0's content, not a tab header

        UI.SendInput(grid, UI.HotKeys.AltRight);     // routed through the grid -> into the nested panel's tunnel

        Assert.Equal(1, panel.SelectedIndex);        // tab switched even though a content button was focused
    }

    [Fact]
    public void FocusedControl_SurfacesFocusedHeader_ForNestedRouting()
    {
        var panel = TwoTabs(out _, out _);
        Assert.Null(panel.FocusedControl);          // nothing focused yet

        panel.Headers[1].Focus();

        Assert.Same(panel.Headers[1], panel.FocusedControl);   // a parent layout routes input here
    }
    #endregion

    #region Orientation
    [Fact]
    public void VerticalBar_RendersStackedHeadersAndContent()
    {
        var panel = TwoTabs(out _, out _, TabBarDock.Left);

        var text = ConsoleSnapshot.ToText(panel, 24, 6);

        Assert.Contains("One", text);
        Assert.Contains("Two", text);
        Assert.Contains("AAAA", text);
    }
    #endregion
}
