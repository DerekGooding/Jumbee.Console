namespace Jumbee.Console.Tests;

using System;
using System.Reflection;

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

    // Point UI's root layout at a test layout (UI.Start normally sets it; here we don't run the loop).
    private static void SetRoot(ILayout? root) =>
        typeof(UI).GetField("layout", BindingFlags.NonPublic | BindingFlags.Static)!.SetValue(null, root);

    [Fact]
    public void CtrlArrows_Recurse_AcrossNestedHorizontalSplitPanels()
    {
        // Replicates the examples shell: three panes as nested horizontal SplitPanels under a status bar. Ctrl+arrows
        // must descend into the nested split to move between panes (dividers are focus stops in between).
        var tree = new Button("tree");
        var middle = new Button("middle");
        var source = new Button("source");
        var status = new TextLabel(TextLabelOrientation.Horizontal, "status");   // non-focusable footer
        var inner = new SplitPanel(SplitOrientation.Horizontal, middle, source, 8);
        var outer = new SplitPanel(SplitOrientation.Horizontal, tree, inner, 8);
        var root = new DockPanel(DockedControlPlacement.Bottom, status, outer);
        ConsoleSnapshot.Render(root, 40, 8);
        SetRoot(root);

        static bool Reach(Button target, Action step)
        {
            for (var i = 0; i < 8 && !target.IsFocused; i++) step();
            return target.IsFocused;
        }

        tree.Focus();
        Assert.True(Reach(middle, UI.FocusRight));   // right crosses into the nested split's first pane
        Assert.True(Reach(source, UI.FocusRight));   // and on to its second pane
        Assert.True(Reach(tree, UI.FocusLeft));      // left crosses all the way back out to the tree
    }

    [Fact]
    public void CtrlArrows_NavigateSpatially_BetweenRootCells_AndWrap()
    {
        var left = new Button("L");
        var right = new Button("R");
        var grid = new Grid([1], [6, 6], [[left, right]]);   // one row, two columns
        ConsoleSnapshot.Render(grid, 12, 1);
        SetRoot(grid);
        left.Focus();

        UI.FocusRight();
        Assert.True(right.IsFocused);     // moved to the cell on the right

        UI.FocusRight();                  // wraps back to the first column
        Assert.True(left.IsFocused);

        UI.FocusLeft();                   // wraps the other way
        Assert.True(right.IsFocused);
    }

    [Fact]
    public void CtrlArrows_SkipCellsWithNoFocusable()
    {
        var a = new Button("A");
        var label = new TextLabel(TextLabelOrientation.Horizontal, "x");   // not interactive -> skipped
        var b = new Button("B");
        var grid = new Grid([1], [4, 4, 4], [[a, label, b]]);
        ConsoleSnapshot.Render(grid, 12, 1);
        SetRoot(grid);
        a.Focus();

        UI.FocusRight();

        Assert.True(b.IsFocused);         // jumped over the non-focusable label cell
    }

    [Fact]
    public void CtrlNP_CycleWithinARegion_ThatIsANestedLayout()
    {
        var one = new Button("1");
        var two = new Button("2");
        var stack = new VerticalStackPanel(one, two);     // a multi-focusable region
        var grid = new Grid([6], [10], [[stack]]);        // the whole root is one cell = the stack
        ConsoleSnapshot.Render(grid, 10, 6);
        SetRoot(grid);
        one.Focus();

        UI.FocusNext();
        Assert.True(two.IsFocused);       // cycled within the stack region

        UI.FocusPrevious();
        Assert.True(one.IsFocused);
    }

    [Fact]
    public void CtrlNP_AreNoOp_InASingleControlCell()
    {
        var only = new Button("only");
        var grid = new Grid([1], [10], [[only]]);
        ConsoleSnapshot.Render(grid, 10, 1);
        SetRoot(grid);
        only.Focus();

        UI.FocusNext();
        Assert.True(only.IsFocused);      // nothing to cycle in a single-control cell -> stays put
    }

    [Fact]
    public void Overlay_WithNoPopup_IsTransparent_DelegatingSpatialNavToTheBottom()
    {
        var left = new Button("L");
        var right = new Button("R");
        var overlay = new Overlay(new Grid([1], [6, 6], [[left, right]]));
        ConsoleSnapshot.Render(overlay, 12, 1);
        SetRoot(overlay);
        left.Focus();

        UI.FocusRight();

        Assert.True(right.IsFocused);     // arrows pass through the (empty) overlay to the bottom grid
    }

    [Fact]
    public void Overlay_ModalPopup_HoldsFocusExclusively_NavCannotLeaveIt()
    {
        var bottom = new Button("bottom");
        var overlay = new Overlay(new Grid([1], [10], [[bottom]]));
        ConsoleSnapshot.Render(overlay, 10, 1);
        SetRoot(overlay);
        bottom.Focus();

        var popup = new Button("popup");
        overlay.ShowModal(popup);
        ConsoleSnapshot.Render(overlay, 10, 1);   // lay the popup out
        Assert.True(popup.IsFocused);
        Assert.False(bottom.IsFocused);

        UI.FocusRight();   // try to navigate out of the modal
        UI.FocusNext();

        Assert.True(popup.IsFocused);     // focus stays on the modal; the bottom is unreachable while it's up
        Assert.False(bottom.IsFocused);
    }
}
