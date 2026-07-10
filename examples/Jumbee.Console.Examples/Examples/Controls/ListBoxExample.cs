namespace Jumbee.Console.Examples;

using System.Collections.Generic;

using Spectre.Console.Rendering;   // IRenderable

using S = Spectre.Console;          // Markup, Text

/// <summary>
/// A scrollable, keyboard-navigable list — it simply <em>is</em> a <see cref="ListBox"/>.
/// Arrows / Home / End / PgUp / PgDn move the selection; it scrolls smoothly when it overflows.
/// Mixes plain text rows with colourful IRenderable rows (status icons) to show that the selection highlight
/// overlays an IRenderable too — keeping each row's own colours under the highlight.
/// </summary>
public sealed class ListBoxExample : ListBox, IExample
{
    public ListBoxExample() : base(BuildItems()) { }

    private static IRenderable[] BuildItems()
    {
        // A few named "services" with a coloured status glyph (an IRenderable), padded out with plain rows so the
        // list overflows and scrolls. The status rows keep their green/amber/red glyph when selected.
        (string Name, string Color, string Glyph, string State)[] services =
        [
            ("api-gateway",   "green",  "●", "online"),
            ("auth-service",  "green",  "●", "online"),
            ("billing",       "yellow", "◐", "degraded"),
            ("search-index",  "red",    "○", "offline"),
            ("cache",         "green",  "●", "online"),
            ("mailer",        "yellow", "◐", "degraded"),
        ];

        var items = new List<IRenderable>();
        foreach (var (name, color, glyph, state) in services)
            items.Add(new S.Markup($"[{color}]{glyph}[/] [bold]{name}[/] [grey]({state})[/]"));

        for (var i = 1; i <= 34; i++)
            items.Add(new S.Text($"Item {i:00}"));

        return items.ToArray();
    }

    #region IExample
    string IExample.Category => "Controls";
    string IExample.Title => "List Box";
    string IExample.Description =>
        "A selectable, keyboard-navigable list with smooth scrolling — the highlight overlays colourful IRenderable rows too.";
    #endregion
}
