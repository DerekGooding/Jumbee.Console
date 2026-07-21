namespace NewsReaderDemo.Core;

/// <summary>Read-state filter, matching eilmeldung's <c>1</c>/<c>2</c>/<c>3</c> scope shortcuts.</summary>
public enum ReadScope { All, Unread, Marked }

/// <summary>What part of the feed tree the current view is scoped to.</summary>
public enum ScopeKind { Library, Folder, Feed, Tag, SavedQuery }

/// <summary>
/// A tree-node selection: the whole library, one folder, one feed, one tag, or a pinned saved query
/// (eilmeldung's <c>query: "Today Unread" today unread</c> style <c>feed_list</c> entries — see
/// docs/configuration.md "Feed List Configuration"). A small value type (not a side-table lookup) so
/// <see cref="ArticleQuery"/> can carry "what's selected" without depending on the tree control.
/// </summary>
public readonly record struct TreeScope(
    ScopeKind Kind, Folder? Folder = null, Feed? Feed = null, string? Tag = null,
    string? SavedQueryName = null, ReadScope SavedReadScope = ReadScope.All, bool SavedToday = false)
{
    public static readonly TreeScope Library = new(ScopeKind.Library);
    public static TreeScope ForFolder(Folder f) => new(ScopeKind.Folder, Folder: f);
    public static TreeScope ForFeed(Feed f) => new(ScopeKind.Feed, Feed: f);
    public static TreeScope ForTag(string t) => new(ScopeKind.Tag, Tag: t);
    public static TreeScope ForSavedQuery(string name, ReadScope readScope, bool today) =>
        new(ScopeKind.SavedQuery, SavedQueryName: name, SavedReadScope: readScope, SavedToday: today);

    public string Label => Kind switch
    {
        ScopeKind.Folder => Folder!.Name,
        ScopeKind.Feed => Feed!.Title,
        ScopeKind.Tag => $"#{Tag}",
        ScopeKind.SavedQuery => SavedQueryName!,
        _ => "All Feeds",
    };
}

/// <summary>
/// The article list's current view: a <see cref="TreeScope"/> (all / a folder / a feed / a tag / a saved query)
/// narrowed by a <see cref="ReadScope"/> and free-text search, applied against a <see cref="Library"/>. This is
/// a small subset of eilmeldung's query language (see docs/queries.md: <c>feed:</c>/<c>tag:</c>/<c>unread</c>/
/// <c>marked</c>/<c>today</c>/free text), not its full regex/time-relative grammar — but it moves the filtering
/// predicate itself out of the view and into Core, where it can be unit-tested and reused without the TUI.
/// </summary>
public class ArticleQuery
{
    public TreeScope TreeScope { get; set; } = TreeScope.Library;
    public ReadScope ReadScope { get; set; } = ReadScope.All;
    public string SearchText { get; set; } = "";

    /// <summary>Returns the articles matching the current scope/read-state/search, newest first.</summary>
    public IEnumerable<Article> Apply(Library library)
    {
        // A saved query (e.g. "Today Unread") is a self-contained preset over the WHOLE library: it owns its
        // own read-scope/today predicate instead of combining with the ambient ReadScope, so selecting it always
        // means exactly what its name says regardless of what scope was active before.
        if (TreeScope.Kind == ScopeKind.SavedQuery)
        {
            var savedSource = library.AllArticles;
            if (TreeScope.SavedToday) savedSource = savedSource.Where(a => a.IsToday);
            savedSource = TreeScope.SavedReadScope switch
            {
                ReadScope.Unread => savedSource.Where(a => !a.IsRead),
                ReadScope.Marked => savedSource.Where(a => a.IsMarked),
                _ => savedSource,
            };
            return ApplySearch(savedSource).OrderByDescending(a => a.Published);
        }

        IEnumerable<Article> source = TreeScope.Kind switch
        {
            ScopeKind.Folder => TreeScope.Folder!.Feeds.SelectMany(f => f.Articles),
            ScopeKind.Feed => TreeScope.Feed!.Articles,
            ScopeKind.Tag => library.AllArticles.Where(a => a.Tags.Contains(TreeScope.Tag!, StringComparer.OrdinalIgnoreCase)),
            _ => library.AllArticles,
        };

        source = ReadScope switch
        {
            ReadScope.Unread => source.Where(a => !a.IsRead),
            ReadScope.Marked => source.Where(a => a.IsMarked),
            _ => source,
        };

        return ApplySearch(source).OrderByDescending(a => a.Published);
    }

    private IEnumerable<Article> ApplySearch(IEnumerable<Article> source) =>
        SearchText is { Length: > 0 } q
            ? source.Where(a =>
                a.Title.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                a.Summary.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                a.Author.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                a.Feed.Title.Contains(q, StringComparison.OrdinalIgnoreCase))
            : source;

    /// <summary>Status-bar summary, e.g. "Tech / Hacker News  ·  unread  ·  "rust"".</summary>
    public string Describe()
    {
        List<string> parts = [TreeScope.Label];
        if (TreeScope.Kind != ScopeKind.SavedQuery && ReadScope != ReadScope.All) parts.Add(ReadScope.ToString().ToLowerInvariant());
        if (SearchText is { Length: > 0 }) parts.Add($"\"{SearchText}\"");
        return string.Join("  ·  ", parts);
    }
}
