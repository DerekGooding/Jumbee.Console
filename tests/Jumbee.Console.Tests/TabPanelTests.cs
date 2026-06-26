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
    public void ArrowKeys_MoveSelection_AlongHorizontalBar()
    {
        var panel = TwoTabs(out _, out _);
        ConsoleSnapshot.Render(panel, 24, 6);
        panel.Headers[0].Focus();

        // Routed exactly as the live path: into the panel, which forwards to the focused header.
        UI.SendInput(panel, new ConsoleKeyInfo('\0', ConsoleKey.RightArrow, false, false, false));
        Assert.Equal(1, panel.SelectedIndex);

        UI.SendInput(panel, new ConsoleKeyInfo('\0', ConsoleKey.LeftArrow, false, false, false));
        Assert.Equal(0, panel.SelectedIndex);
    }

    [Fact]
    public void UpDownArrows_MoveSelection_AlongVerticalBar()
    {
        var panel = TwoTabs(out _, out _, TabBarDock.Left);
        ConsoleSnapshot.Render(panel, 24, 6);
        panel.Headers[0].Focus();

        UI.SendInput(panel, new ConsoleKeyInfo('\0', ConsoleKey.DownArrow, false, false, false));
        Assert.Equal(1, panel.SelectedIndex);

        UI.SendInput(panel, new ConsoleKeyInfo('\0', ConsoleKey.UpArrow, false, false, false));
        Assert.Equal(0, panel.SelectedIndex);
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
