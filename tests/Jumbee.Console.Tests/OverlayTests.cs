namespace Jumbee.Console.Tests;

using System;

using ConsoleGUI.Input;

using Jumbee.Console;
using Jumbee.Console.Snapshot;

using Xunit;

using CColor = ConsoleGUI.Data.Color;

public class OverlayTests
{
    private static (Overlay overlay, Button bottom) MakeOverlay()
    {
        var bottom = new Button("bottom");
        var grid = new Grid([3], [10], [[bottom]]);
        return (new Overlay(grid), bottom);
    }

    private static UI.InputEventArgs KeyArgs(ConsoleKey key) =>
        new(new InputEvent(new ConsoleKeyInfo('\0', key, shift: false, alt: false, control: false)));

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
    public void CloseKey_ClosesPopup_BeforeItReachesThePopup()
    {
        var (overlay, _) = MakeOverlay();
        overlay.Show(new Button("popup"));
        Assert.True(overlay.IsShowing);

        overlay.OnInput(KeyArgs(ConsoleKey.Escape));   // default CloseKey

        Assert.False(overlay.IsShowing);
    }

    [Fact]
    public void CloseKey_Null_DoesNotClose()
    {
        var (overlay, _) = MakeOverlay();
        overlay.CloseKey = null;
        overlay.Show(new Button("popup"));

        overlay.OnInput(KeyArgs(ConsoleKey.Escape));

        Assert.True(overlay.IsShowing);
    }

    [Fact]
    public void ShowModal_IsModal_AndCompositesPopup()
    {
        var (overlay, _) = MakeOverlay();
        var popup = new Button("MODALX");

        overlay.ShowModal(popup);

        Assert.True(overlay.IsShowing);
        Assert.True(overlay.IsModal);
        Assert.Contains("MODALX", ConsoleSnapshot.ToText(overlay, 40, 12));
    }

    [Fact]
    public void ModalScrim_ShowsBottomThroughDimmed()
    {
        var (overlay, _) = MakeOverlay();              // the bottom layer reads "bottom" (top-left, away from centre)
        overlay.ShowModal(new Button("MODALX"));       // default ModalDim is see-through

        var text = ConsoleSnapshot.ToText(overlay, 40, 12);

        Assert.Contains("MODALX", text);               // the popup
        Assert.Contains("bottom", text);               // the layer behind shows through the dimmed scrim
    }

    [Fact]
    public void ModalScrim_DimsContent_AndVeilsTransparentCells_NotBlack()
    {
        // The exact path F1 help uses: UI.ShowHelp() compiles the help and calls overlay.ShowModal(...). Here we
        // snapshot the composed overlay and evaluate the scrim's actual cell colours.
        UI.StyleTheme = new DefaultStyleTheme();   // deterministic: tint (10,10,15), dim 0.6

        // Bottom layer: a label with an explicit background at the top-left; everything else is transparent.
        var label = new TextLabel(TextLabelOrientation.Horizontal, "AB", CColor.White, new CColor(200, 100, 50));
        var overlay = new Overlay(new Grid([1], [6], [[label]]));

        overlay.ShowModal(new Button("OK"));

        var buf = ConsoleSnapshot.Render(overlay, 30, 10);

        var tint = new CColor(10, 10, 15);       // DefaultStyleTheme.Scrim (ModalDim 0.6)

        // 1) A transparent scrim cell (no control, away from the popup) is filled with the tint — a real colour, so it
        //    renders as that colour rather than the terminal-default (which a null background would emit as black).
        var backdrop = buf[25, 8].Background!.Value;
        Assert.Equal(tint, backdrop);
        Assert.NotNull(buf[25, 8].Background);   // a concrete colour, not a null/transparent (default-bg) cell

        // 2) A real background dims toward the tint: content shows through darkened — between its colour and the tint.
        var content = buf[0, 0].Background!.Value;
        Assert.Equal(new CColor(200, 100, 50).Mix(tint, 0.6f), content);
        Assert.True(content.Red is > 15 and < 200, $"content red {content.Red} should be dimmed, not original/black");
        Assert.Equal('A', buf[0, 0].Content);

        // 3) The popup is composited verbatim over the scrim (its text is present).
        Assert.Contains("OK", ConsoleSnapshot.ToText(buf));
    }

    [Fact]
    public void ModalScrim_ComposesTransparentPopupCells_OverScrim_NeverNullBackground()
    {
        UI.StyleTheme = new DefaultStyleTheme();
        var overlay = new Overlay(new Grid([1], [6], [[new TextLabel(TextLabelOrientation.Horizontal, "x")]]));
        // A popup whose text has no background of its own (transparent). Without composition over the scrim its cells
        // would carry a null background, which the renderer emits as the terminal default (black).
        overlay.ShowModal(new TextLabel(TextLabelOrientation.Horizontal, "POPUP", CColor.White));

        var buf = ConsoleSnapshot.Render(overlay, 30, 10);

        // Under the modal, every glyph is composed over the scrim — none renders on a null (default-black) background.
        for (var y = 0; y < buf.Size.Height; y++)
            for (var x = 0; x < buf.Size.Width; x++)
            {
                var cell = buf[x, y];
                if (cell.Content is char ch && ch != ' ' && ch != '\0')
                    Assert.True(cell.Background is not null, $"glyph '{ch}' at {x},{y} has a null (default-black) background");
            }
    }

    [Fact]
    public void ModalPopup_DoesNotCloseOnFocusLoss()
    {
        var (overlay, bottom) = MakeOverlay();
        overlay.ShowModal(new Button("popup"));

        UI.SetFocus(bottom);                 // a modal must not be dismissed by focus moving away

        Assert.True(overlay.IsShowing);
    }

    [Fact]
    public void ModalPopup_ClosesOnCloseKey()
    {
        var (overlay, _) = MakeOverlay();
        overlay.ShowModal(new Button("popup"));

        overlay.OnInput(KeyArgs(ConsoleKey.Escape));

        Assert.False(overlay.IsShowing);
        Assert.False(overlay.IsModal);
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
