namespace Jumbee.Console.Examples;

using System.Collections.Generic;
using System.Linq;

using Spectre.Console.Rendering;   // IRenderable

using S = Spectre.Console;          // Markup

/// <summary>
/// Two ListBoxes stacked in a vertical <see cref="SplitPanel"/> (drag the divider to resize):
/// a plain-text list with ordinary-width highlighting and smooth scrolling on top, and rich multi-line markup cards
/// (bold titles, icons, grey descriptions) with full-width highlighting below. Each list scrolls independently and
/// is focused by clicking it; arrows move its selection.
/// </summary>
public sealed class ListBoxExample : SplitPanel, IExample
{
    public ListBoxExample() : base(SplitOrientation.Vertical, BuildPlainList(), BuildCardList(), splitPosition: 9) { }

    // Top pane: ordinary text rows — default (item-width) highlight, and enough rows to scroll.
    private static IFocusable BuildPlainList() =>
        new ListBox(Enumerable.Range(1, 30).Select(i => $"Item {i:00}").ToArray())
            .WithFrame(title: "Text items · item-width highlight");

    // Bottom pane: multi-line markup cards with full-width highlighting — the selected card reads as a full-width bar
    // spanning all of its rows.
    private static IFocusable BuildCardList()
    {
        var cards = new ListBox { HighlightFullWidth = true };
        cards.AddItems(Cards.Select(BuildCard));
        return cards.WithFrame(title: "Rich cards · full-width highlight");
    }

    // An icon + bold title, a grey description beneath, and a trailing blank line so each card has some breathing room
    // (the blank row is part of the item, so it too is highlighted when the card is selected).
    private static IRenderable BuildCard((string Icon, string Title, string Desc) c) =>
        new S.Markup($"{c.Icon}  [bold]{S.Markup.Escape(c.Title)}[/]\n     [grey]{S.Markup.Escape(c.Desc)}[/]\n");

    private static readonly (string Icon, string Title, string Desc)[] Cards =
    [
        ("✦", "Fade To Colors",      "Fades to the specified colors"),
        ("◑", "Fade To Foreground",  "Fades to the specified foreground color"),
        ("☀", "Fade From Foreground","Fades from the specified foreground color"),
        ("✿", "HSL Shift",           "Changes hue, saturation, and lightness"),
        ("❂", "HSL Shift 2",         "A colour-cycling effect"),
        ("⚡", "Sparkle",             "A twinkling star-field overlay"),
        ("≈", "Wave",                "A sinusoidal ripple across the text"),
        ("★", "Pulse",               "A rhythmic brightness pulse"),
        ("❄", "Freeze",              "Frost creeps in from the edges"),
        ("♦", "Dissolve",            "Characters scatter and reform"),
    ];

    #region IExample
    string IExample.Category => "Controls";
    string IExample.Title => "List Box";
    string IExample.Description =>
        "Two lists — plain text with item-width highlighting and scrolling, and rich multi-line markup cards with full-width highlighting.";
    // Show the ListBox source (not SplitPanel, our structural base) beside the example.
    IReadOnlyList<string> IExample.SourceFiles => ["ListBoxExample.cs", "ListBox.cs"];
    #endregion
}
