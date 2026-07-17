namespace Jumbee.Console.AgentHarnessDemo;

using System.Collections.Generic;

using Jumbee.Console;

/// <summary>The left rail of the harness: a small nav section, a "Recents" header, the scrollable session list, and
/// a docked account row at the bottom. The caller frames and places <see cref="Layout"/>.</summary>
internal sealed class Sidebar
{
    #region Constructors
    public Sidebar(IEnumerable<SessionItem> sessions)
    {
        var nav = BuildNav();
        var header = BuildHeader();
        _sessions = BuildSessions(sessions);
        var account = BuildAccount();

        // Stacked top-to-bottom: nav, the "Recents" header, the session list (fill), and the account row pinned to
        // the bottom — nested DockPanels, mirroring the IdeDemo idiom (a docked edge gets its intrinsic/explicit
        // height, the rest fills).
        _layout = new DockPanel(DockedControlPlacement.Bottom, account,
                      new DockPanel(DockedControlPlacement.Top, nav,
                          new DockPanel(DockedControlPlacement.Top, header, _sessions)));
    }
    #endregion

    #region Properties
    /// <summary>The composed, unframed sidebar content — the caller frames and places it.</summary>
    public ILayout Layout => _layout;

    /// <summary>The recents list, exposed so the app can hook <see cref="ListBox.SelectionChanged"/>.</summary>
    public ListBox Sessions => _sessions;
    #endregion

    #region Methods
    // Nav rows: a tinted glyph + a normal-text label, one Markup per row. Explicit Height so the section docks to its
    // four rows instead of filling (a bare RenderableControl reports no intrinsic height under a finite parent).
    private static SpectreControl<Spectre.Console.Rows> BuildNav()
    {
        var glyph = (Style)Palette.Coral;
        var label = (Style)Palette.Text;
        (string Glyph, string Label)[] rows =
        [
            ("⌂", "Home"),
            ("◳", "Artifacts"),
            ("✎", "Customize"),
            ("…", "More"),
        ];

        var markups = new Spectre.Console.Markup[rows.Length];
        for (var i = 0; i < rows.Length; i++)
            markups[i] = new Spectre.Console.Markup($" {Frag(glyph, rows[i].Glyph)}  {Frag(label, rows[i].Label)}");

        return Section(rows.Length, markups);
    }

    // A blank spacer row above the muted "Recents" header.
    private static SpectreControl<Spectre.Console.Rows> BuildHeader() => Section(2,
        new Spectre.Console.Markup(""),
        new Spectre.Console.Markup($"  {Frag((Style)Palette.TextFaint, "Recents")}"));

    private ListBox BuildSessions(IEnumerable<SessionItem> sessions)
    {
        var list = new ListBox
        {
            HighlightFullWidth = true,
            SelectedBackgroundColor = Palette.RaisedBg,
            SelectedForegroundColor = Palette.Text,
        };

        var text = (Style)Palette.Text;
        var warn = (Style)Palette.Yellow;
        var active = 0;
        var i = 0;
        foreach (var s in sessions)
        {
            // Warn rows get a yellow "⚠ " prefix; plain rows pad by the same width so titles line up.
            var markup = s.Warn
                ? $" {Frag(warn, "⚠")} {Frag(text, s.Title)}"
                : $"   {Frag(text, s.Title)}";
            list.AddItem(new Spectre.Console.Markup(markup));
            if (s.Active) active = i;
            i++;
        }

        list.SelectedIndex = active;
        // A borderless frame gives the list a scroll viewport (a ListBox only scrolls through its Frame).
        list.WithFrame(BorderStyle.None);
        return list;
    }

    // Bottom account row: a bold coral avatar glyph, the name, then a muted plan suffix.
    private static SpectreControl<Spectre.Console.Rows> BuildAccount() => Section(1,
        new Spectre.Console.Markup(
            $" {Frag((Style)Palette.Coral | Style.Bold, "A")}  {Frag((Style)Palette.Text, "Allister")} {Frag((Style)Palette.TextMuted, "· Max")}"));

    // A fixed-height passive section wrapping static Markup rows. The explicit Height docks it correctly; Focusable is
    // off so it is never a tab/focus target.
    private static SpectreControl<Spectre.Console.Rows> Section(int height, params Spectre.Console.Markup[] rows) =>
        new SpectreControl<Spectre.Console.Rows>(new Spectre.Console.Rows(rows)) { Focusable = false, Height = height };

    // A styled, markup-escaped fragment: [<style tokens>]text[/].
    private static string Frag(Style style, string text) =>
        $"[{style.ToMarkup()}]{Spectre.Console.Markup.Escape(text)}[/]";
    #endregion

    #region Fields
    private readonly DockPanel _layout;
    private readonly ListBox _sessions;
    #endregion
}
