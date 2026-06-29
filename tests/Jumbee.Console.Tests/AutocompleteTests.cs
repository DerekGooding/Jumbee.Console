namespace Jumbee.Console.Tests;

using ConsoleGUI.Input;
using ConsoleGUI.Space;

using Jumbee.Console.Snapshot;

using Xunit;

/// <summary>Headless tests for <see cref="Autocomplete"/>: type-ahead filtering, the passive (non-focus-stealing)
/// popup, keyboard accept/navigate/dismiss, and mouse-click accept.</summary>
public class AutocompleteTests
{
    private static readonly string[] Headers =
        ["User-Agent", "Accept", "Accept-Encoding", "Authorization", "Content-Type", "Cache-Control"];

    private static ConsoleKeyInfo Type(char c) => new(c, (ConsoleKey)char.ToUpperInvariant(c), false, false, false);
    private static ConsoleKeyInfo K(ConsoleKey k) => new('\0', k, false, false, false);

    private static (TextInput input, Overlay overlay, Autocomplete ac) Build()
    {
        var input = new TextInput();
        var overlay = new Overlay(new Grid([1], [40], [[input]]));
        var ac = new Autocomplete(input, overlay, Headers);
        ConsoleSnapshot.Render(overlay, 40, 12);   // size the field
        UI.SetFocus(input);
        return (input, overlay, ac);
    }

    private static void TypeText(TextInput input, string s)
    {
        foreach (var c in s) UI.SendInput(input, Type(c));
    }

    [Fact]
    public void Typing_ShowsMatchingSuggestions()
    {
        var (input, overlay, _) = Build();
        TypeText(input, "acc");

        Assert.True(overlay.IsShowing);
        var text = ConsoleSnapshot.ToText(overlay, 40, 12);
        Assert.Contains("Accept", text);
        Assert.Contains("Accept-Encoding", text);
        Assert.DoesNotContain("User-Agent", text);   // doesn't match "acc"
    }

    [Fact]
    public void NoMatches_DoesNotShowPopup()
    {
        var (input, overlay, _) = Build();
        TypeText(input, "zzz");
        Assert.False(overlay.IsShowing);
    }

    [Fact]
    public void Enter_AcceptsHighlightedSuggestion_AndCloses()
    {
        var (input, overlay, _) = Build();
        TypeText(input, "use");        // matches "User-Agent"
        UI.SendInput(input, K(ConsoleKey.Enter));

        Assert.Equal("User-Agent", input.Text);
        Assert.False(overlay.IsShowing);
    }

    [Fact]
    public void DownThenEnter_AcceptsSecondSuggestion()
    {
        var (input, overlay, _) = Build();
        TypeText(input, "acc");        // "Accept", "Accept-Encoding"
        UI.SendInput(input, K(ConsoleKey.DownArrow));
        UI.SendInput(input, K(ConsoleKey.Enter));

        Assert.Equal("Accept-Encoding", input.Text);
    }

    [Fact]
    public void Escape_DismissesButKeepsText()
    {
        var (input, overlay, _) = Build();
        TypeText(input, "acc");
        UI.SendInput(input, K(ConsoleKey.Escape));

        Assert.False(overlay.IsShowing);
        Assert.Equal("acc", input.Text);
    }

    [Fact]
    public void Popup_DoesNotStealFocus_FieldKeepsEditing()
    {
        var (input, overlay, _) = Build();
        TypeText(input, "ac");
        Assert.True(overlay.IsShowing);
        Assert.True(input.IsFocused);   // the passive popup did not take focus

        TypeText(input, "c");           // keep typing -> field still receives keys
        Assert.Equal("acc", input.Text);
        Assert.True(overlay.IsShowing);
    }

    [Fact]
    public void Click_AcceptsClickedSuggestion()
    {
        var (input, overlay, _) = Build();
        TypeText(input, "acc");
        ConsoleSnapshot.Render(overlay, 40, 12);   // size the popup so its rows have coordinates

        var ml = (IMouseListener)overlay.Top!;     // the suggestion list
        ml.OnMouseDown(new Position(0, 1));         // second row -> "Accept-Encoding"
        ml.OnMouseUp(new Position(0, 1));

        Assert.Equal("Accept-Encoding", input.Text);
        Assert.False(overlay.IsShowing);
    }

    [Fact]
    public void AcceptingExactMatch_DoesNotReopen()
    {
        var (input, overlay, _) = Build();
        TypeText(input, "use");
        UI.SendInput(input, K(ConsoleKey.Enter));   // accepts "User-Agent" (now the text exactly equals a candidate)
        Assert.False(overlay.IsShowing);            // must not immediately reopen for the accepted value
    }
}
