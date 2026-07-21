using NewsReaderDemo.Core;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace NewsReaderDemo.App.Controls;

/// <summary>
/// One article-list row as a real Spectre <see cref="IRenderable"/>.
/// </summary>
/// <remarks>
/// <para>
/// <b>Round 4 fix (the vanished "feed · age" column, for real this time)</b>: round 3 computed the row's
/// right-aligned column against the <c>maxWidth</c> parameter <see cref="Render"/> is called with — but
/// diagnosing against the app's REAL wiring (not a synthetic probe) showed <see cref="Jumbee.Console.ListBox"/>
/// invokes an item's <see cref="Render"/> with a generous, constant probe width (observed: 1000), not the
/// column's actual on-screen width. Padding the row out to that probe width, then handing it to the ListBox to
/// blit into the real (much narrower) column, meant every row's trailing content — the right column, last in the
/// markup string — landed past the real buffer's right edge and was silently dropped during the blit. Nothing in
/// the public <c>Jumbee.Console.ListBox</c>/<c>ListBox.ListBoxItem</c> XML docs says what width item content is
/// actually rendered at vs. measured at; this was only found by instrumenting the row's own <c>Build</c> and
/// comparing against <see cref="ConsoleSnapshot"/> output of the real app (see round-4 report). The fix: never
/// trust <c>maxWidth</c> alone for column math — clamp it against the owning <see cref="Jumbee.Console.ListBox"/>'s
/// own <see cref="Jumbee.Console.Control.ActualWidth"/> (documented as "the control's actual laid-out width in
/// cells"), which IS reliable once the list has been through a layout pass. <see cref="ArticleListPanel"/> wires
/// this in as <c>() => List.ActualWidth</c>.
/// </para>
/// <para>
/// <b>Cache-key freshness (round 4)</b>: the cache key now includes <see cref="Article.Age"/> (a cheap, already
/// pre-formatted string like "3h") alongside read/marked/flagged/width, so a long-running session doesn't freeze
/// an article's displayed age at whatever it was on first render — the row cache invalidates exactly when the
/// age label would actually change.
/// </para>
/// <para>
/// <b>Measured-width columns</b>: every width used for column math comes from
/// <see cref="StringExtensions.GetCellWidth(string)"/> (public Spectre.Console API), never <c>.Length</c>, so a
/// row full of wide glyphs still fits its budget exactly.
/// </para>
/// It still holds a live reference to its <see cref="Article"/>, so replacing a
/// <see cref="Jumbee.Console.ListBox.ListBoxItem.Content"/> with a fresh <see cref="ArticleRow"/> after a
/// read/mark toggle updates exactly that one row.
/// </remarks>
public sealed class ArticleRow(Article article, Func<int>? widthProvider = null) : IRenderable
{
    public Article Article => article;

    // Single consistent teal for every tag pill (round 2 hash-picked a color per tag, which read as noise rather
    // than a signature accent) — matches the reviewer's "consistent teal tag pills" ask.
    private const string TagColor = "teal";
    private const int MinTitleWidth = 6;

    private (bool read, bool marked, bool flagged, string age, int width) _cacheKey = (false, false, false, "", -1);
    private List<Segment>? _cachedSegments;

    public Measurement Measure(RenderOptions options, int maxWidth) => new(Math.Min(maxWidth, 12), maxWidth);

    public IEnumerable<Segment> Render(RenderOptions options, int maxWidth)
    {
        // ListBox does not call Render at the item's real on-screen width (see class remarks) — clamp against
        // the owning list's own ActualWidth, which IS trustworthy once laid out. Fall back to maxWidth itself
        // when ActualWidth isn't known yet (e.g. before the first layout pass, or no provider wired).
        var actual = widthProvider?.Invoke() ?? 0;
        var effectiveWidth = actual > 0 ? Math.Min(maxWidth, actual) : maxWidth;

        var key = (article.IsRead, article.IsMarked, article.IsFlagged, article.Age, effectiveWidth);
        if (_cachedSegments is null || key != _cacheKey)
        {
            _cachedSegments = [.. ((IRenderable)Build(article, effectiveWidth)).Render(options, effectiveWidth)];
            _cacheKey = key;
        }
        return _cachedSegments;
    }

    private static Markup Build(Article a, int width)
    {
        // Neutral white unread dot (round 2 used green, which read as a status/success color rather than
        // eilmeldung's plain unread marker); hollow grey dot for read.
        var dotGlyph = a.IsRead ? "○" : "●";
        var dot = a.IsRead ? "[grey58]○[/]" : "[bold white]●[/]";
        var flagGlyph = a.IsFlagged ? "⚑" : " ";
        var starGlyph = a.IsMarked ? "★" : " ";
        var flag = a.IsFlagged ? $"[#ffb000]{flagGlyph}[/]" : " ";
        var star = a.IsMarked ? $"[#ffb000]{starGlyph}[/]" : " ";
        var titleStyle = a.IsRead ? "grey62" : "bold white";

        var tagsPlain = a.Tags.Count > 0 ? " " + string.Join(" ", a.Tags.Select(t => $" {t} ")) : "";
        var tagsMarkup = a.Tags.Count > 0
            ? " " + string.Join(" ", a.Tags.Select(t => $"[black on {TagColor}] {Markup.Escape(t)} [/]"))
            : "";

        var rightPlain = $"{a.Feed.Title} · {a.Age}";
        var rightWidth = rightPlain.GetCellWidth();

        // Fixed-width prefix: dot + gap + flag + star + gap. Every piece here is a single glyph column, but we
        // still measure it instead of assuming 1 cell so a future glyph swap can't silently break alignment.
        var prefixWidth = dotGlyph.GetCellWidth() + 1 + flagGlyph.GetCellWidth() + starGlyph.GetCellWidth() + 1;
        var tagsWidth = tagsPlain.GetCellWidth();
        var fixedWidth = prefixWidth + tagsWidth;

        // Budget the title against what's ACTUALLY left after the fixed prefix, tags, a one-cell gap, and the
        // right column — never against a guess. If there isn't enough room to show a readable title AND the
        // right column, drop the right column instead of letting Spectre's own overflow truncation eat it.
        var titleBudgetWithRight = width - fixedWidth - 1 - rightWidth;
        bool showRight = titleBudgetWithRight >= MinTitleWidth;

        string title;
        int pad;
        if (showRight)
        {
            title = TruncateToWidth(a.Title, titleBudgetWithRight);
            var leftWidth = fixedWidth + title.GetCellWidth();
            pad = Math.Max(1, width - leftWidth - rightWidth);
        }
        else
        {
            var soloBudget = Math.Max(0, width - fixedWidth);
            title = TruncateToWidth(a.Title, soloBudget);
            var leftWidth = fixedWidth + title.GetCellWidth();
            pad = Math.Max(0, width - leftWidth);
            rightPlain = "";
        }

        var rightSegment = rightPlain.Length > 0 ? $"[#9370db]{Markup.Escape(rightPlain)}[/]" : "";
        return new Markup($"{dot} {flag}{star} [{titleStyle}]{Markup.Escape(title)}[/]{tagsMarkup}{new string(' ', pad)}{rightSegment}");
    }

    // Cell-width-aware truncation (not char-count): walks glyphs, summing GetCellWidth, and stops before the
    // budget would be exceeded — so a row full of wide glyphs still fits maxWidth exactly instead of overrunning
    // it by however many extra cells those glyphs cost beyond 1-per-char.
    private static string TruncateToWidth(string text, int maxCells)
    {
        if (maxCells <= 0) return "";
        if (text.GetCellWidth() <= maxCells) return text;
        var used = 0;
        var i = 0;
        var ellipsisWidth = "…".GetCellWidth();
        var budget = Math.Max(0, maxCells - ellipsisWidth);
        for (; i < text.Length; i++)
        {
            var w = text[i].GetCellWidth();
            if (used + w > budget) break;
            used += w;
        }
        return text[..i] + (i < text.Length ? "…" : "");
    }
}
