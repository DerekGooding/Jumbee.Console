namespace Jumbee.Console.Tests;

using Jumbee.Console;
using Jumbee.Console.Snapshot;

using Xunit;

public class OverlayTests
{
    private static (Overlay overlay, Button bottom) MakeOverlay()
    {
        var bottom = new Button("bottom");
        var grid = new Grid([3], [10], [[bottom]]);
        return (new Overlay(grid), bottom);
    }

    [Fact]
    public void Show_CompositesPopup_AndFocusesIt()
    {
        var (overlay, _) = MakeOverlay();
        var popup = new Button("popup");

        Assert.False(overlay.IsShowing);

        overlay.Show(popup);

        Assert.True(overlay.IsShowing);
        Assert.Same(popup, overlay.Top);
        Assert.True(popup.IsFocused);
        Assert.NotNull(overlay.control.TopContent);
    }

    [Fact]
    public void Hide_ClearsPopup_AndRestoresPreviousFocus()
    {
        var (overlay, bottom) = MakeOverlay();
        UI.SetFocus(bottom);                 // bottom is the focused control before the popup opens
        Assert.True(bottom.IsFocused);

        var popup = new Button("popup");
        overlay.Show(popup);
        Assert.True(popup.IsFocused);
        Assert.False(bottom.IsFocused);

        overlay.Hide();

        Assert.False(overlay.IsShowing);
        Assert.Null(overlay.control.TopContent);
        Assert.True(bottom.IsFocused);       // focus restored
    }

    [Fact]
    public void ClickingOutside_ClosesPopup_WhenCloseOnFocusLost()
    {
        var (overlay, bottom) = MakeOverlay();
        var popup = new Button("popup");
        overlay.Show(popup);
        Assert.True(overlay.IsShowing);

        UI.SetFocus(bottom);                 // simulates a click landing on the bottom layer

        Assert.False(overlay.IsShowing);     // auto-closed
        Assert.True(bottom.IsFocused);       // the click's focus is kept (not stolen back)
    }

    [Fact]
    public void ClickingOutside_KeepsPopup_WhenCloseOnFocusLostDisabled()
    {
        var (overlay, bottom) = MakeOverlay();
        overlay.CloseOnFocusLost = false;
        var popup = new Button("popup");
        overlay.Show(popup);

        UI.SetFocus(bottom);

        Assert.True(overlay.IsShowing);
    }

    [Fact]
    public void Snapshot_ShowThenHide_RemovesPopupFromComposite()
    {
        var (overlay, _) = MakeOverlay();
        var popup = new Button("DIALOGTEXT");

        overlay.Show(popup);
        var shown = ConsoleSnapshot.ToText(overlay, 40, 12);
        Assert.Contains("DIALOGTEXT", shown);

        overlay.Hide();
        var hidden = ConsoleSnapshot.ToText(overlay, 40, 12);
        Assert.DoesNotContain("DIALOGTEXT", hidden);
    }

    [Fact]
    public void Show_Anchored_CompositesPopup()
    {
        var (overlay, _) = MakeOverlay();
        var popup = new Button("popup");

        overlay.Show(popup, x: 2, y: 1);

        Assert.True(overlay.IsShowing);
        Assert.NotNull(overlay.control.TopContent);
    }
}
