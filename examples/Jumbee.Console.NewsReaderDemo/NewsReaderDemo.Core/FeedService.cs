using System.Text.RegularExpressions;
using CodeHollow.FeedReader;

namespace NewsReaderDemo.Core;

/// <summary>A folder-name + feed-name + feed-url triple describing what to subscribe to.</summary>
public record FeedSubscription(string Folder, string Name, string Url);

/// <summary>
/// Fetches and parses feeds via CodeHollow.FeedReader and builds a <see cref="Library"/>.
/// All network/parsing work is async so callers can run it off the UI thread.
/// </summary>
public partial class FeedService
{
    /// <summary>Fetches every subscription concurrently and returns a populated <see cref="Library"/>.
    /// A feed that fails to load is skipped (kept out of the tree) rather than failing the whole sync.</summary>
    public async Task<Library> LoadAsync(IReadOnlyList<FeedSubscription> subscriptions, CancellationToken ct = default)
    {
        var byFolder = subscriptions.GroupBy(s => s.Folder);
        var folders = new List<Folder>();

        foreach (var group in byFolder)
        {
            var feedTasks = group.Select(sub => FetchFeedAsync(sub, ct)).ToList();
            var feeds = await Task.WhenAll(feedTasks);
            folders.Add(new Folder { Name = group.Key, Feeds = [.. feeds.Where(f => f is not null)!] });
        }

        return new Library { Folders = folders };
    }

    private static async Task<Feed?> FetchFeedAsync(FeedSubscription sub, CancellationToken ct)
    {
        try
        {
            var parsed = await FeedReader.ReadAsync(sub.Url, ct);
            var feed = new Feed { Title = sub.Name, Url = sub.Url };
            feed.Articles.AddRange(parsed.Items.Select(item => ToArticle(item, feed)));
            return feed;
        }
        catch
        {
            // Network/parse failure: skip this feed rather than crashing the sync.
            return null;
        }
    }

    private static Article ToArticle(FeedItem item, Feed feed) => new()
    {
        Id = item.Id ?? item.Link ?? item.Title ?? Guid.NewGuid().ToString(),
        Title = StripHtml(item.Title) is { Length: > 0 } t ? t : "(untitled)",
        Author = item.Author ?? "",
        Summary = StripHtml(item.Description ?? ""),
        Body = StripHtml(item.Description ?? item.Content ?? ""),
        Link = item.Link ?? "",
        ImageUrl = ExtractImageUrl(item.Description) ?? ExtractImageUrl(item.Content) ?? "",
        Published = item.PublishingDate.HasValue ? new DateTimeOffset(DateTime.SpecifyKind(item.PublishingDate.Value, DateTimeKind.Utc)) : null,
        Feed = feed,
        // RSS/Atom <category> elements, surfaced as eilmeldung-style tags for the tree's "Tags" section and
        // tag pills in the article list. Capped so a feed that categorizes heavily doesn't dominate a row.
        Tags = [.. (item.Categories ?? []).Where(c => !string.IsNullOrWhiteSpace(c)).Select(c => c.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).Take(4)],
    };

    // Feed descriptions are frequently raw HTML; strip tags for the plain-text reader body/summary.
    private static string StripHtml(string input)
    {
        if (string.IsNullOrEmpty(input)) return "";
        var sb = new System.Text.StringBuilder(input.Length);
        var inTag = false;
        foreach (var c in input)
        {
            if (c == '<') { inTag = true; continue; }
            if (c == '>') { inTag = false; continue; }
            if (!inTag) sb.Append(c);
        }
        return System.Net.WebUtility.HtmlDecode(sb.ToString()).Trim();
    }

    private static string? ExtractImageUrl(string? html) =>
        !string.IsNullOrEmpty(html) && ImgSrcRegex().Match(html) is { Success: true } m ? m.Groups[1].Value : null;

    [GeneratedRegex("""<img[^>]+src=["']([^"']+)["']""", RegexOptions.IgnoreCase)]
    private static partial Regex ImgSrcRegex();
}
