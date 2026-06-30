namespace Jumbee.Console.Tests;

using System.Linq;

using ConsoleGUI.Input;

using Jumbee.Console.Snapshot;

using Xunit;

/// <summary>Regression: accepting an autocomplete suggestion that fits the field must render it from the first
/// column (the leading char was previously scrolled off to seat the end-of-text caret). Mirrors the demo's
/// 26-wide Header field.</summary>
public class AcceptedSuggestionRenderTests
{
    private static ConsoleKeyInfo Type(char c) => new(c, (ConsoleKey)char.ToUpperInvariant(c), false, false, false);
    private static ConsoleKeyInfo K(ConsoleKey k) => new('\0', k, false, false, false);

    [Fact]
    public void AcceptedSuggestion_RendersFromFirstColumn()
    {
        var keyInput = new TextInput(placeholder: "Header");
        var addRow = new Grid([1], [26, 40, 8],
            [[keyInput, new TextInput(placeholder: "Value"), new Button("Add")]]);
        var overlay = new Overlay(new Grid([1, 1], [82], [[addRow], [new TextLabel(TextLabelOrientation.Horizontal, "", Color.White)]]));
        UI.Overlay = overlay;   // ambient host (headless: no UI.Start)
        var ac = new Autocomplete(keyInput, "Authorization", "Accept", "Content-Type");

        ConsoleSnapshot.Render(overlay, 90, 6);
        UI.SetFocus(keyInput);
        Assert.Equal(26, keyInput.ActualWidth);

        foreach (var c in "auth") UI.SendInput(keyInput, Type(c));
        UI.SendInput(keyInput, K(ConsoleKey.Enter));   // accept "Authorization"
        Assert.Equal("Authorization", keyInput.Text);

        var buf = ConsoleSnapshot.Render(overlay, 90, 6);
        var line = string.Concat(Enumerable.Range(0, 13).Select(x => buf[x, 0].Content ?? ' '));
        Assert.Equal("Authorization", line);   // first char 'A' present, not scrolled off
    }
}
