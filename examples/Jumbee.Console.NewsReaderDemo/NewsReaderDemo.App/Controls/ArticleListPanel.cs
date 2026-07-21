using NewsReaderDemo.Core;
using Jumbee.Console;

namespace NewsReaderDemo.App.Controls;

/// <summary>
/// The top-right article list: wraps a <see cref="ListBox"/> with domain-aware operations (populate from a
/// filtered article set, resolve the selected <see cref="Article"/>) so <c>Program.cs</c> never builds row
/// strings or indexes into the domain list directly.
/// </summary>
/// <remarks>
/// Round 4: <see cref="Jumbee.Console.ListBox.ListBoxItem.Tag"/> (new in 0.1.4) now carries the row's
/// <see cref="Article"/> directly, so <see cref="SelectedArticle"/> reads it straight off the selected item
/// instead of round 3's parallel <c>_articles</c> list indexed by <c>List.SelectedIndex</c> — no more risk of the
/// two lists drifting out of sync. <c>_articles</c> is kept only as the ordered result set for
/// <see cref="Articles"/> (status bar counts, etc).
/// <para>
/// Round 3's single state-mutation flow is unchanged: EVERY article state change (read/mark) goes through
/// <c>NewsReaderDemoApp</c>'s <c>MutateArticle</c>, which mutates the <see cref="Article"/> and then calls
/// <see cref="Populate"/> again — the query is re-applied every time, so the visible set can never drift from
/// what the active scope says it should be. <see cref="Populate"/> accepts a <c>preferredSelection</c> so that
/// re-apply doesn't reset the cursor to the top when the mutated article is still in the result set.
/// </para>
/// Each row's <see cref="Jumbee.Console.ListBox.ListBoxItem.Content"/> is an <see cref="ArticleRow"/> wired with
/// <c>() =&gt; List.ActualWidth</c> so it can right-align the "feed · age" column against the list's real
/// on-screen width instead of trusting whatever probe width <see cref="Jumbee.Console.ListBox"/> calls the item's
/// <c>Render</c> with (see <see cref="ArticleRow"/> remarks).
/// </remarks>
public class ArticleListPanel
{
    public ListBox List { get; } = new();
    private readonly List<Article> _articles = [];

    public IReadOnlyList<Article> Articles => _articles;

    public Article? SelectedArticle => List.SelectedItem?.Tag as Article;

    public event EventHandler<Article?>? SelectionChanged;

    public ArticleListPanel()
    {
        List.HighlightFullWidth = true;
        List.SelectionChanged += (_, _) => SelectionChanged?.Invoke(this, SelectedArticle);
    }

    /// <summary>Rebuilds the visible rows from <paramref name="articles"/> — the single path for both a new
    /// scope/search AND a per-article state change (see remarks). <paramref name="preferredSelection"/>, if still
    /// present in the new set, keeps the cursor on it instead of resetting to the top; otherwise the previous
    /// index is clamped into range (or 0 for an empty-to-nonempty transition).</summary>
    public void Populate(IEnumerable<Article> articles, Article? preferredSelection = null)
    {
        var previousIndex = List.SelectedIndex;

        _articles.Clear();
        _articles.AddRange(articles);
        List.Clear();
        foreach (var a in _articles)
            List.AddItem(new ArticleRow(a, () => List.ActualWidth)).Tag = a;

        if (_articles.Count == 0)
        {
            SelectionChanged?.Invoke(this, null);
            return;
        }

        var targetIndex = preferredSelection is not null ? _articles.IndexOf(preferredSelection) : -1;
        if (targetIndex < 0) targetIndex = Math.Clamp(previousIndex, 0, _articles.Count - 1);

        // ListBox.SelectedIndex may already equal targetIndex (e.g. both 0 on a freshly cleared list), which is a
        // no-op and never raises SelectionChanged — so the reader pane would stay on stale content until the next
        // key press. Fire the event explicitly whenever the index lands somewhere, matching eilmeldung's
        // auto-select behaviour.
        List.SelectedIndex = targetIndex;
        SelectionChanged?.Invoke(this, SelectedArticle);
    }
}
