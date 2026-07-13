namespace Jumbee.Console.Tests;

using Jumbee.Console;
using Jumbee.Console.Snapshot;

using Xunit;

using Decoration = ConsoleGUI.Data.Decoration;

/// <summary>The themed default focus cue: a focused control that isn't framed and doesn't indicate focus its own way
/// gets an automatic background tint (<see cref="IStyleTheme.Focus"/>), so keyboard focus is always visible.</summary>
public class FocusIndicatorTests
{
    private static ConsoleGUI.Data.Color Tint => UI.StyleTheme.Focus.BackgroundColor!.Value.ToConsoleGUIColor();

    [Fact]
    public void FocusedUnframedControl_GetsTheDefaultFocusTint()
    {
        var list = new ListBox("a", "b", "c");   // ListBox doesn't render its own focus, so it opts into the default

        var unfocused = ConsoleSnapshot.Render(list, 10, 4);
        Assert.Null(unfocused[0, 3].Background);         // an empty row is untinted while unfocused

        list.Focus();
        var focused = ConsoleSnapshot.Render(list, 10, 4);
        Assert.Equal(Tint, focused[0, 3].Background);    // focus now tints the control's (unpainted) cells
    }

    [Fact]
    public void ControlThatRendersItsOwnFocus_IsNotTinted()
    {
        var button = new Button("OK");   // Button opts out (RendersOwnFocus): focus lightens the tile instead
        button.Focus();

        var buf = ConsoleSnapshot.Render(button, 12, 3);

        for (var x = 0; x < buf.Size.Width; x++)
            for (var y = 0; y < buf.Size.Height; y++)
                Assert.NotEqual(Tint, buf[x, y].Background);
    }

    [Fact]
    public void VisiblyFramedControl_ShowsTheFrameBorder_NotTheTint()
    {
        var list = new ListBox("a", "b", "c");
        list.WithFrame(borderStyle: BorderStyle.Rounded);   // a visible border recolours on focus → no tint
        list.Focus();

        var buf = ConsoleSnapshot.Render(list, 12, 6);

        for (var x = 0; x < buf.Size.Width; x++)
            for (var y = 0; y < buf.Size.Height; y++)
                Assert.NotEqual(Tint, buf[x, y].Background);
    }

    [Fact]
    public void BorderlessFramedControl_StillGetsTheTint()
    {
        // A borderless frame (used e.g. to give a control a scroll viewport without a visible box) shows no focus
        // border — so the wrapped control must fall back to the default tint rather than have no cue at all.
        var list = new ListBox("a", "b", "c");
        list.WithFrame(borderStyle: BorderStyle.None);
        list.Focus();

        var buf = ConsoleSnapshot.Render(list, 12, 6);

        var tinted = false;
        for (var x = 0; x < buf.Size.Width && !tinted; x++)
            for (var y = 0; y < buf.Size.Height && !tinted; y++)
                if (Equals(buf[x, y].Background, Tint)) tinted = true;
        Assert.True(tinted, "borderless-framed focused control should show the default focus tint");
    }

    // Test themes that pick an alternate focus cue mode (everything else default).
    private sealed class RingFocusTheme : IStyleTheme { public FocusStyle FocusStyle => FocusStyle.Ring; }
    private sealed class UnderlineFocusTheme : IStyleTheme { public FocusStyle FocusStyle => FocusStyle.Underline; }
    private sealed class FocusBorderTheme : IStyleTheme { public BorderStyle? FocusedFrameBorder => BorderStyle.Rounded; }

    [Fact]
    public void FocusOnlyBorderTheme_ShowsBorder_AndSuppressesTint() => WithTheme(new FocusBorderTheme(), () =>
    {
        var list = new ListBox("a", "b", "c");
        list.WithFrame(borderStyle: BorderStyle.None);   // borderless at rest; the theme adds a border on focus
        list.Focus();
        var buf = ConsoleSnapshot.Render(list, 12, 6);

        Assert.True(buf[0, 0].Content is { } c && c != ' ', "the focus border corner glyph should be drawn");
        for (var x = 0; x < buf.Size.Width; x++)
            for (var y = 0; y < buf.Size.Height; y++)
                Assert.NotEqual(Tint, buf[x, y].Background);   // the border is the cue, not the tint
    });

    [Fact]
    public void FramedComposite_ShowsFocusBorder_WhenNestedDescendantIsFocused() => WithTheme(new FocusBorderTheme(), () =>
    {
        // A framed composite-of-composites (MultiTabCodeEditor → tabbed CodeEditors): focusing the deeply-nested inner
        // editor does NOT change the outer composite's own IsFocused (SetOwnership stops at the nested composite, and
        // the Owner chain doesn't reach it), so the outer frame must light via the "contains focus" path
        // (UI.FocusChanged) rather than the direct one.
        var editor = new MultiTabCodeEditor(Language.CSharp);
        editor.WithFrame(borderStyle: BorderStyle.None);   // borderless at rest; the theme adds a border only on focus
        var doc = editor.OpenDocument("a.cs", "class A { }");

        var unfocused = ConsoleSnapshot.Render(editor, 40, 10);
        Assert.True(unfocused[0, 0].Content is null or ' ', "no border corner while nothing inside is focused");

        UI.SetFocus(doc.Editor);   // focus the deeply-nested inner TextEditor
        var focused = ConsoleSnapshot.Render(editor, 40, 10);
        Assert.True(focused[0, 0].Content is { } c && c != ' ',
            "the outer frame should show its focus border when a nested descendant is focused");
    });

    private static void WithTheme(IStyleTheme theme, System.Action body)
    {
        var prev = UI.StyleTheme;
        UI.StyleTheme = theme;
        try { body(); } finally { UI.StyleTheme = prev; }
    }

    [Fact]
    public void FocusStyle_Ring_TintsOnlyTheEdgeCells() => WithTheme(new RingFocusTheme(), () =>
    {
        var list = new ListBox("a", "b", "c");   // constructed under the ring theme -> captures Ring
        list.Focus();
        var buf = ConsoleSnapshot.Render(list, 10, 5);

        Assert.Equal(Tint, buf[0, 4].Background);      // an empty bottom-left edge cell is tinted (the ring)
        Assert.NotEqual(Tint, buf[3, 2].Background);   // an interior empty cell is not
    });

    [Fact]
    public void FocusStyle_Underline_UnderlinesTheBottomRow() => WithTheme(new UnderlineFocusTheme(), () =>
    {
        var list = new ListBox("a", "b", "c");
        list.Focus();
        var buf = ConsoleSnapshot.Render(list, 10, 5);

        Assert.True((buf[0, 4].Character.Decoration ?? Decoration.None).HasFlag(Decoration.Underline));    // bottom row
        Assert.False((buf[0, 2].Character.Decoration ?? Decoration.None).HasFlag(Decoration.Underline));   // interior row
    });
}
