namespace Jumbee.Console.Tests;

using ConsoleGUI.Input;
using ConsoleGUI.Space;

using Jumbee.Console;
using Jumbee.Console.Snapshot;

using Xunit;

public class ToggleTests
{
    private static readonly Position Origin = new(0, 0);

    private static void Click(Control c, Position p)
    {
        var m = (IMouseListener)c;
        m.OnMouseDown(p);
        m.OnMouseUp(p);
    }

    #region Checkbox
    [Fact]
    public void Checkbox_Click_TogglesAndRaisesChanged()
    {
        var cb = new Checkbox("Accept");
        bool? changed = null;
        cb.Changed += (_, v) => changed = v;

        Click(cb, Origin);
        Assert.True(cb.IsChecked);
        Assert.True(changed);

        Click(cb, Origin);
        Assert.False(cb.IsChecked);
        Assert.False(changed);
    }

    [Theory]
    [InlineData(ConsoleKey.Enter)]
    [InlineData(ConsoleKey.Spacebar)]
    public void Checkbox_EnterOrSpace_Toggles(ConsoleKey key)
    {
        var cb = new Checkbox("Accept");

        UI.SendInput(cb, key);

        Assert.True(cb.IsChecked);
    }

    [Fact]
    public void Checkbox_RendersStateGlyphAndLabel()
    {
        var cb = new Checkbox("Accept");

        Assert.Contains("[ ] Accept", ConsoleSnapshot.ToText(cb, 20, 1));

        cb.IsChecked = true;
        Assert.Contains("[X] Accept", ConsoleSnapshot.ToText(cb, 20, 1));
    }
    #endregion

    #region RadioButton
    [Fact]
    public void RadioButton_Click_LatchesOnAndStaysOn()
    {
        var rb = new RadioButton("Option");
        var changes = 0;
        rb.Changed += (_, _) => changes++;

        Click(rb, Origin);
        Assert.True(rb.IsChecked);

        Click(rb, Origin);            // a second click does not turn a radio off
        Assert.True(rb.IsChecked);
        Assert.Equal(1, changes);     // only changed once
    }

    [Fact]
    public void RadioButton_RendersStateGlyph()
    {
        var rb = new RadioButton("One") { IsChecked = true };
        Assert.Contains("(●) One", ConsoleSnapshot.ToText(rb, 20, 1));
    }
    #endregion

    #region Switch
    [Fact]
    public void Switch_Click_Toggles()
    {
        var sw = new Switch("Dark mode");

        Click(sw, Origin);
        Assert.True(sw.IsChecked);

        Click(sw, Origin);
        Assert.False(sw.IsChecked);
    }

    [Fact]
    public void Switch_RendersDistinctGlyphPerState()
    {
        var sw = new Switch("Power");
        var off = ConsoleSnapshot.ToText(sw, 20, 1);

        sw.IsChecked = true;
        var on = ConsoleSnapshot.ToText(sw, 20, 1);

        Assert.NotEqual(off, on);
        Assert.Contains("Power", on);
    }
    #endregion

    #region RadioSet
    [Fact]
    public void RadioSet_ArrowThenSpace_SelectsHighlightedOption()
    {
        var rs = new RadioSet("Red", "Green", "Blue");
        var changed = -1;
        rs.SelectionChanged += (_, i) => changed = i;

        UI.SendInput(rs, ConsoleKey.DownArrow);   // cursor 0 -> 1
        UI.SendInput(rs, ConsoleKey.Spacebar);

        Assert.Equal(1, rs.SelectedIndex);
        Assert.Equal("Green", rs.SelectedValue);
        Assert.Equal(1, changed);
    }

    [Fact]
    public void RadioSet_Click_SelectsClickedRow()
    {
        var rs = new RadioSet("Red", "Green", "Blue");

        Click(rs, new Position(0, 2));   // third row

        Assert.Equal(2, rs.SelectedIndex);
        Assert.Equal("Blue", rs.SelectedValue);
    }

    [Fact]
    public void RadioSet_SelectingAnother_IsMutuallyExclusive()
    {
        var rs = new RadioSet("Red", "Green", "Blue") { SelectedIndex = 0 };

        rs.SelectedIndex = 2;

        Assert.Equal(2, rs.SelectedIndex);
        Assert.Equal("Blue", rs.SelectedValue);
    }

    [Fact]
    public void RadioSet_RendersSelectedMarker()
    {
        var rs = new RadioSet("Red", "Green", "Blue") { SelectedIndex = 1 };

        var text = ConsoleSnapshot.ToText(rs, 20, 3);

        Assert.Contains("(●) Green", text);
        Assert.Contains("( ) Red", text);
    }
    #endregion

    #region SelectionList
    [Fact]
    public void SelectionList_SpaceToggles_AllowsMultiple()
    {
        var sl = new SelectionList("A", "B", "C");

        UI.SendInput(sl, ConsoleKey.Spacebar);    // check row 0
        UI.SendInput(sl, ConsoleKey.DownArrow);
        UI.SendInput(sl, ConsoleKey.Spacebar);    // check row 1

        Assert.Equal(new[] { 0, 1 }, sl.SelectedIndices);
        Assert.Equal(new[] { "A", "B" }, sl.SelectedValues);
    }

    [Fact]
    public void SelectionList_SpaceTwice_UnchecksRow()
    {
        var sl = new SelectionList("A", "B");

        UI.SendInput(sl, ConsoleKey.Spacebar);    // check row 0
        UI.SendInput(sl, ConsoleKey.Spacebar);    // uncheck row 0

        Assert.Empty(sl.SelectedIndices);
    }

    [Fact]
    public void SelectionList_Click_TogglesClickedRow()
    {
        var sl = new SelectionList("A", "B", "C");
        var changed = -1;
        sl.SelectionChanged += (_, i) => changed = i;

        Click(sl, new Position(0, 1));   // toggle row 1

        Assert.True(sl.IsCheckedAt(1));
        Assert.Equal(1, changed);
    }

    [Fact]
    public void SelectionList_RendersCheckedMarkers()
    {
        var sl = new SelectionList("A", "B", "C");
        sl.SetChecked(0, true);
        sl.SetChecked(2, true);

        var text = ConsoleSnapshot.ToText(sl, 20, 3);

        Assert.Contains("[X] A", text);
        Assert.Contains("[ ] B", text);
        Assert.Contains("[X] C", text);
    }
    #endregion
}
