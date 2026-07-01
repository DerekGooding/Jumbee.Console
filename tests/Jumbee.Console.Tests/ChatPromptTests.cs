namespace Jumbee.Console.Tests;

using System;

using Jumbee.Console;
using Jumbee.Console.Snapshot;

using Xunit;

/// <summary>Headless tests for <see cref="ChatPrompt"/>: the prompt-glyph / busy-spinner gutter, focus delegation to
/// the input, submit/change relaying, and the attached type-ahead.</summary>
public class ChatPromptTests
{
    private static ConsoleKeyInfo Type(char c) => new(c, (ConsoleKey)char.ToUpperInvariant(c), false, false, false);
    private static ConsoleKeyInfo K(ConsoleKey k) => new('\0', k, false, false, false);

    private static void TypeText(IFocusable target, string s)
    {
        foreach (var c in s) UI.SendInput(target, Type(c));
    }

    #region Rendering
    [Fact]
    public void RendersPromptGlyphAndPlaceholder()
    {
        var chat = new ChatPrompt(placeholder: "Ask me anything");

        var text = ConsoleSnapshot.ToText(chat, 40, 1);

        Assert.Contains("❯", text);                 // the idle prompt glyph in the left gutter
        Assert.Contains("Ask me anything", text);    // the field's muted placeholder
    }

    [Fact]
    public void RendersTypedText_AfterTheGutter()
    {
        var chat = new ChatPrompt();
        chat.Input.Focus();
        TypeText(chat.FocusedControl!, "hello");

        var text = ConsoleSnapshot.ToText(chat, 40, 1);

        Assert.Contains("❯", text);        // gutter still present
        Assert.Contains("hello", text);    // ...to the left of the typed text
        Assert.StartsWith("❯ hello", text.TrimEnd('\n'));
    }

    [Fact]
    public void CustomPromptGlyph_IsUsed()
    {
        var chat = new ChatPrompt { Prompt = ">>>" };

        var text = ConsoleSnapshot.ToText(chat, 40, 1);

        Assert.Contains(">>>", text);
    }
    #endregion

    #region Busy spinner
    [Fact]
    public void Busy_ShowsSpinnerFrame_InsteadOfPromptGlyph()
    {
        var chat = new ChatPrompt { Busy = true };

        var text = ConsoleSnapshot.ToText(chat, 40, 1);

        Assert.DoesNotContain("❯", text);   // the static prompt glyph is replaced...
        // ...by a spinner frame (Dots frame 0 is a braille glyph).
        Assert.Contains(Spectre.Console.Spinner.Known.Dots.Frames[0], text);
    }

    [Fact]
    public void ClearingBusy_RestoresThePromptGlyph()
    {
        var chat = new ChatPrompt { Busy = true };
        ConsoleSnapshot.Render(chat, 40, 1);   // render once while busy

        chat.Busy = false;
        var text = ConsoleSnapshot.ToText(chat, 40, 1);

        Assert.Contains("❯", text);
    }
    #endregion

    #region Focus + input routing
    [Fact]
    public void FocusChild_IsTheInput()
    {
        var chat = new ChatPrompt();
        Assert.Null(chat.FocusedControl);

        UI.SetFocus(chat);   // focus the composite (the navigable unit)

        Assert.True(chat.IsFocused);
        Assert.True(chat.Input.IsFocused);          // delegated inward to the field
        Assert.Same(chat.Input, chat.FocusedControl);
    }

    [Fact]
    public void GutterIsNotFocusable()
    {
        var chat = new ChatPrompt();
        UI.SetFocus(chat);

        // The only focusable descendant is the input; focus never lands on the gutter adornment.
        Assert.Same(chat.Input, chat.FocusedControl);
    }

    [Fact]
    public void Input_RoutedThroughFocusedControl_ReachesField()
    {
        var chat = new ChatPrompt();
        chat.Input.Focus();

        UI.SendInput(chat.FocusedControl!, Type('X'));

        Assert.Equal("X", chat.Text);
    }
    #endregion

    #region Events
    [Fact]
    public void Enter_RaisesSubmitted_WithTheText()
    {
        var chat = new ChatPrompt();
        string? submitted = null;
        chat.Submitted += (_, s) => submitted = s;

        chat.Input.Focus();
        TypeText(chat.FocusedControl!, "run tests");
        UI.SendInput(chat.FocusedControl!, K(ConsoleKey.Enter));

        Assert.Equal("run tests", submitted);
    }

    [Fact]
    public void Typing_RaisesChanged()
    {
        var chat = new ChatPrompt();
        var changes = 0;
        chat.Changed += (_, _) => changes++;

        chat.Input.Focus();
        TypeText(chat.FocusedControl!, "hi");

        Assert.Equal(2, changes);
    }
    #endregion

    #region Framed in a layout (live routing path)
    // The controls above are driven with SendInput straight to the field; these exercise the real app path — a framed
    // ChatPrompt nested in a Grid inside the ambient Overlay, with keyboard routed through the ROOT layout's OnInput
    // (as UI does for live keys). This is what catches frame-overhead / routing bugs that a direct-to-field test can't.
    private static (ChatPrompt chat, Overlay overlay) BuildFramedInGrid()
    {
        var chat = new ChatPrompt(placeholder: "Send a message…");
        chat.WithRoundedBorder();                       // rounded border, no title -> one content row fits a 3-row cell
        var transcript = new Log().WithRoundedBorder();
        var overlay = new Overlay(new Grid([20, 3], [78], [[transcript], [chat]]));
        UI.Overlay = overlay;
        ConsoleSnapshot.Render(overlay, 82, 25);
        return (chat, overlay);
    }

    [Fact]
    public void FramedInGrid_RendersInputRow()
    {
        var (_, overlay) = BuildFramedInGrid();

        // The single input row must survive the frame + grid nesting (a titled frame would eat it — see the demo note).
        Assert.Contains("Send a message", ConsoleSnapshot.ToText(overlay, 82, 25));
    }

    [Fact]
    public void FramedInGrid_KeyboardRoutedThroughRootLayout_ReachesTheField()
    {
        var (chat, overlay) = BuildFramedInGrid();
        UI.SetFocus(chat);

        // Route through the root layout exactly like UI.OnInput does for a live keypress (not SendInput-to-target).
        overlay.OnInput(new UI.InputEventArgs(new ConsoleGUI.Input.InputEvent(Type('Z'))));

        Assert.Equal("Z", chat.Text);
    }
    #endregion

    #region Autocomplete
    [Fact]
    public void WithSuggestions_ShowsMatchingTypeAhead()
    {
        var chat = new ChatPrompt();
        var overlay = new Overlay(new Grid([1], [40], [[chat]]));
        UI.Overlay = overlay;                    // ambient host the suggestions float into
        chat.WithSuggestions("/help", "/clear", "/model", "/quit");
        ConsoleSnapshot.Render(overlay, 40, 8);
        UI.SetFocus(chat.Input);

        TypeText(chat.Input, "/c");

        Assert.True(overlay.IsShowing);
        var text = ConsoleSnapshot.ToText(overlay, 40, 8);
        Assert.Contains("/clear", text);
        Assert.DoesNotContain("/help", text);    // doesn't match "/c"
    }
    #endregion
}
