namespace Jumbee.Console.Tests;

using ConsoleGUI.Input;
using ConsoleGUI.Space;

using Jumbee.Console;
using Jumbee.Console.Snapshot;

using Xunit;

public class SelectTests
{
    #region ListBox option-list behavior
    [Fact]
    public void ListBox_SelectedIndex_ClampsAndRaisesSelectionChanged()
    {
        var lb = new ListBox("A", "B", "C");
        var changed = -1;
        lb.SelectionChanged += (_, i) => changed = i;

        lb.SelectedIndex = 2;
        Assert.Equal(2, lb.SelectedIndex);
        Assert.Equal(2, changed);
        Assert.Equal("C", lb.SelectedItem?.Text);

        lb.SelectedIndex = 99;          // clamps to last
        Assert.Equal(2, lb.SelectedIndex);
    }

    [Fact]
    public void ListBox_Enter_CommitsSelectedItem()
    {
        var lb = new ListBox("A", "B", "C") { SelectedIndex = 1 };
        ListBox.ListBoxItem? committed = null;
        lb.Committed += (_, item) => committed = item;

        UI.SendInput(lb, ConsoleKey.Enter);

        Assert.Equal("B", committed?.Text);
    }

    [Fact]
    public void ListBox_Escape_RaisesCancelled()
    {
        var lb = new ListBox("A", "B");
        var cancelled = false;
        lb.Cancelled += (_, _) => cancelled = true;

        UI.SendInput(lb, ConsoleKey.Escape);

        Assert.True(cancelled);
    }

    [Fact]
    public void ListBox_Click_SelectsRowAndCommits()
    {
        var lb = new ListBox("A", "B", "C");
        ListBox.ListBoxItem? committed = null;
        lb.Committed += (_, item) => committed = item;

        var m = (IMouseListener)lb;
        m.OnMouseDown(new Position(0, 1));
        m.OnMouseUp(new Position(0, 1));   // row 1 -> "B"

        Assert.Equal(1, lb.SelectedIndex);
        Assert.Equal("B", committed?.Text);
    }
    #endregion

    #region Select
    private static Overlay HostOverlay() => new(new Grid([1], [10], [[new Button("host")]]));

    [Fact]
    public void Select_Closed_RendersPlaceholderAndArrow()
    {
        var select = new Select("Red", "Green", "Blue");

        var text = ConsoleSnapshot.ToText(select, 20, 1);

        Assert.Contains("Select", text);   // placeholder
        Assert.Contains("▼", text);
    }

    [Fact]
    public void Select_Open_ShowsDropdownWithOptions()
    {
        var overlay = HostOverlay();
        var select = new Select("Red", "Green", "Blue") { Overlay = overlay };

        select.Open();

        Assert.True(overlay.IsShowing);
        Assert.IsType<ListBox>(overlay.Top);
        var dropdown = (ListBox)overlay.Top!;
        var text = ConsoleSnapshot.ToText(dropdown, 20, 8);
        Assert.Contains("Green", text);
    }

    [Fact]
    public void Select_CommitFromDropdown_SetsValueClosesAndRaisesChange()
    {
        var overlay = HostOverlay();
        var select = new Select("Red", "Green", "Blue") { Overlay = overlay };
        string? changed = null;
        select.SelectionChanged += (_, v) => changed = v;

        select.Open();
        var dropdown = (ListBox)overlay.Top!;
        dropdown.SelectedIndex = 1;                 // Green
        UI.SendInput(dropdown, ConsoleKey.Enter);

        Assert.Equal("Green", select.SelectedValue);
        Assert.Equal("Green", changed);
        Assert.False(overlay.IsShowing);            // closed after commit
    }

    [Fact]
    public void Select_EscapeInDropdown_ClosesWithoutChanging()
    {
        var overlay = HostOverlay();
        var select = new Select("Red", "Green", "Blue") { Overlay = overlay };

        select.Open();
        UI.SendInput((ListBox)overlay.Top!, ConsoleKey.Escape);

        Assert.False(overlay.IsShowing);
        Assert.Null(select.SelectedValue);
    }

    [Fact]
    public void Select_Open_WithoutOverlayHost_IsNoOp()
    {
        var select = new Select("Red", "Green");

        select.Open();   // no Overlay set

        Assert.Null(select.SelectedValue);
    }
    #endregion
}
