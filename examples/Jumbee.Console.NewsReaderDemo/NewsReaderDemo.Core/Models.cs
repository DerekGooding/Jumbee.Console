namespace NewsReaderDemo.Core;

/// <summary>A single RSS/Atom article, decoupled from any feed-library type.</summary>
public class Article
{
    public required string Id { get; init; }
    public required string Title { get; init; }
    public string Author { get; init; } = "";
    public string Summary { get; init; } = "";
    public string Body { get; init; } = "";
    public string Link { get; init; } = "";
    public string ImageUrl { get; init; } = "";
    public DateTimeOffset? Published { get; init; }
    public required Feed Feed { get; init; }
    public List<string> Tags { get; init; } = [];

    public bool IsRead { get; set; }
    public bool IsMarked { get; set; }
    public bool IsFlagged { get; set; }

    /// <summary>True if published within the last 24 hours — the predicate behind eilmeldung's <c>today</c>
    /// query keyword and the pinned "Today Unread"/"Today Marked" saved queries.</summary>
    public bool IsToday => Published is { } p && DateTimeOffset.UtcNow - p < TimeSpan.FromHours(24);

    /// <summary>Human "age" label, e.g. "3h", "2d", matching eilmeldung's compact age column.</summary>
    public string Age
    {
        get
        {
            if (Published is not { } published) return "-";
            // DateTimeOffset subtraction is offset-safe either way, but UtcNow (matching how FeedService always
            // constructs Published as a UTC-offset DateTimeOffset) keeps both sides of this subtraction in the
            // same frame of reference instead of mixing local-now with a UTC timestamp.
            var span = DateTimeOffset.UtcNow - published;
            return span switch
            {
                { TotalMinutes: < 1 } => "now",
                { TotalHours: < 1 } => $"{(int)span.TotalMinutes}m",
                { TotalDays: < 1 } => $"{(int)span.TotalHours}h",
                { TotalDays: < 30 } => $"{(int)span.TotalDays}d",
                _ => $"{(int)(span.TotalDays / 30)}mo",
            };
        }
    }
}

/// <summary>A subscribed feed, grouped under a <see cref="Folder"/>.</summary>
public class Feed
{
    public required string Title { get; init; }
    public required string Url { get; init; }
    public List<Article> Articles { get; init; } = [];

    public int UnreadCount => Articles.Count(a => !a.IsRead);
}

/// <summary>A category/folder grouping feeds, as shown in eilmeldung's left-hand tree.</summary>
public class Folder
{
    public required string Name { get; init; }
    public List<Feed> Feeds { get; init; } = [];

    public int UnreadCount => Feeds.Sum(f => f.UnreadCount);
}

/// <summary>The full subscription set: folders on the left, plus all articles flattened for list/search views.</summary>
public class Library
{
    public List<Folder> Folders { get; init; } = [];

    public IEnumerable<Article> AllArticles => Folders.SelectMany(f => f.Feeds).SelectMany(f => f.Articles);

    /// <summary>Distinct tags across every article, for the tree's "Tags" section.</summary>
    public IEnumerable<string> AllTags =>
        AllArticles.SelectMany(a => a.Tags).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(t => t, StringComparer.OrdinalIgnoreCase);
}
