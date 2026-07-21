using NewsReaderDemo.Core;
using Spectre.Console;
using Spectre.Console.Rendering;
using JTree = Jumbee.Console.Tree;
using JColor = Jumbee.Console.Color;

namespace NewsReaderDemo.App.Controls;

/// <summary>
/// Builds and maintains a Jumbee <see cref="JTree"/> from a <see cref="Library"/>: two pinned saved-query rows
/// ("Today Unread" / "Today Marked", eilmeldung's default <c>feed_list</c> — see docs/configuration.md), an "All
/// Feeds" row, a folder/feed section, and a "Tags" section — each kind drawn with its own colored icon glyph
/// instead of a text label, and unread counts as bare italic numbers (no parentheses), matching eilmeldung's tree.
/// </summary>
/// <remarks>
/// <para>
/// <b>Round 4 — connector-less tree</b>: 0.1.4 added <see cref="Jumbee.Console.TreeGuide.None"/> ("no connector
/// lines — hierarchy is shown by indentation (and any node glyphs) alone", per the package XML), which is exactly
/// eilmeldung's icon-driven tree with no branch/guide lines at all. This replaces round 3's workaround (picking
/// <see cref="Jumbee.Console.TreeGuide.Ascii"/> and driving <c>guideStyle</c> down to a near-background grey to
/// make the lines recede) with the real thing — a documented mode, not an approximation.
/// </para>
/// <para>
/// <b>Round 4 — no more tofu</b>: round 3's icon set (◆ ▣ ▤ ◎ ★ for feed/folder/tags/all-feeds/saved-query) are
/// Miscellaneous-Symbols/Geometric-Shapes glyphs the review's target terminal font doesn't cover, rendering as
/// tofu boxes. There is no documented API to query glyph coverage for an arbitrary font (a
/// <c>Control.SupportsGlyph(string)</c>-shaped capability doesn't exist in the public surface — see round-4
/// capability-questions), so this round drops to the ASCII fallback the milestone brief explicitly allows for
/// leaves (<c>*</c>, <c>@</c>, <c>-</c>, <c>#</c>), and reuses the library's OWN default disclosure glyphs
/// (<see cref="Jumbee.Console.IGlyphTheme.TreeExpanded"/>/<see cref="Jumbee.Console.IGlyphTheme.TreeCollapsed"/>,
/// documented as <c>"▼ "</c>/<c>"► "</c>) for folder/tags parent nodes instead of overriding them — those are the
/// one pair of non-ASCII glyphs the library itself ships as its baseline, so they're the safest non-ASCII bet
/// available without file/font access. Differentiation between folder vs. tags now comes from
/// <see cref="Jumbee.Console.Tree.TreeNode.DisclosureGlyphColor"/> alone.
/// </para>
/// <para>
/// <b>Round 4 — Tag replaces the side dictionary</b>: <see cref="JTree.TreeNode.Tag"/> (new in 0.1.4, "an
/// object? slot for arbitrary application data ... so a node/row can map back to its domain object without a
/// side dictionary") now carries each node's <see cref="TreeScope"/> directly. Round 1–3's <c>_scopes</c>
/// dictionary — the thing the round-1/round-3 remarks called out as "still how a consumer has to bridge node
/// identity to domain identity" — is gone; <see cref="CurrentScope"/> is a one-line <c>SelectedNode?.Tag as
/// TreeScope?</c> read.
/// </para>
/// </remarks>
public class FeedTreeBuilder
{
    // Leaf icons: ASCII, guaranteed to render in any font (see class remarks on the round-3 tofu finding).
    // Parent (folder/tags) nodes use the library's own default disclosure glyphs (▼/►) instead of an override.
    private const string SavedQueryIcon = "* ";
    private const string AllFeedsIcon = "@ ";
    private const string FeedIcon = "- ";
    private const string TagIcon = "# ";

    private static readonly JColor Purple = Spectre.Console.Color.FromHex("#9370db");
    private static readonly JColor Amber = Spectre.Console.Color.FromHex("#ffb000");
    private static readonly JColor Gold = Spectre.Console.Color.FromHex("#e6c200");
    private static readonly JColor Teal = Spectre.Console.Color.Teal;

    public JTree Tree { get; } = new(new Markup("[bold]Feeds[/]"), guide: Jumbee.Console.TreeGuide.None, expanded: true);

    private Library _library = new();

    /// <summary>Rebuilds the tree's children from <paramref name="library"/>.</summary>
    public void Populate(Library library)
    {
        _library = library;
        foreach (var child in Tree.Root.Children.ToArray())
            Tree.RemoveNode(child);

        var todayUnread = Tree.Root.AddChild(SavedQueryLabel("Today Unread"), "Today Unread");
        todayUnread.LeafGlyph = SavedQueryIcon;
        todayUnread.LeafGlyphColor = Gold;
        todayUnread.Tag = TreeScope.ForSavedQuery("Today Unread", ReadScope.Unread, today: true);

        var todayMarked = Tree.Root.AddChild(SavedQueryLabel("Today Marked"), "Today Marked");
        todayMarked.LeafGlyph = SavedQueryIcon;
        todayMarked.LeafGlyphColor = Gold;
        todayMarked.Tag = TreeScope.ForSavedQuery("Today Marked", ReadScope.Marked, today: true);

        var allNode = Tree.Root.AddChild(new Markup("[bold #9370db]All Feeds[/]"), "All Feeds");
        allNode.LeafGlyph = AllFeedsIcon;
        allNode.LeafGlyphColor = Purple;
        allNode.Tag = TreeScope.Library;

        foreach (var folder in library.Folders)
        {
            var folderNode = Tree.Root.AddChild(FolderLabel(folder), folder.Name);
            folderNode.Expanded = true;
            folderNode.DisclosureGlyphColor = Purple;
            folderNode.Tag = TreeScope.ForFolder(folder);
            foreach (var feed in folder.Feeds)
            {
                var feedNode = folderNode.AddChild(FeedLabel(feed), feed.Title);
                feedNode.LeafGlyph = FeedIcon;
                feedNode.LeafGlyphColor = Teal;
                feedNode.Tag = TreeScope.ForFeed(feed);
            }
        }

        var tags = library.AllTags.ToList();
        if (tags.Count > 0)
        {
            var tagsNode = Tree.Root.AddChild(new Markup("[bold teal]Tags[/]"), "Tags");
            tagsNode.Expanded = true;
            tagsNode.DisclosureGlyphColor = Teal;
            foreach (var tag in tags)
            {
                var tagNode = tagsNode.AddChild(new Markup(Markup.Escape(tag)), tag);
                tagNode.LeafGlyph = TagIcon;
                tagNode.LeafGlyphColor = Teal;
                tagNode.Tag = TreeScope.ForTag(tag);
            }
        }
    }

    /// <summary>Re-renders folder/feed/saved-query unread counts in place (via <see cref="JTree.TreeNode.Label"/>,
    /// which is mutable) after a read/mark toggle, instead of a full <see cref="Populate"/> rebuild that would
    /// reset expansion/selection.</summary>
    public void RefreshCounts()
    {
        foreach (var node in AllNodes(Tree.Root))
        {
            if (node.Tag is not TreeScope scope) continue;
            node.Label = scope.Kind switch
            {
                ScopeKind.Folder => FolderLabel(scope.Folder!),
                ScopeKind.Feed => FeedLabel(scope.Feed!),
                ScopeKind.SavedQuery => SavedQueryLabel(scope.SavedQueryName!, SavedQueryCount(scope)),
                _ => node.Label,
            };
        }
    }

    /// <summary>The domain scope for the tree's currently highlighted node, or <see langword="null"/> for an
    /// unmapped/absent selection. Reads <see cref="JTree.TreeNode.Tag"/> directly (0.1.4) instead of a side
    /// dictionary — see class remarks.</summary>
    public TreeScope? CurrentScope() => Tree.SelectedNode?.Tag as TreeScope?;

    private static IEnumerable<JTree.TreeNode> AllNodes(JTree.TreeNode node)
    {
        yield return node;
        foreach (var child in node.Children)
            foreach (var n in AllNodes(child))
                yield return n;
    }

    private int SavedQueryCount(TreeScope scope)
    {
        var source = _library.AllArticles;
        if (scope.SavedToday) source = source.Where(a => a.IsToday);
        return scope.SavedReadScope switch
        {
            ReadScope.Unread => source.Count(a => !a.IsRead),
            ReadScope.Marked => source.Count(a => a.IsMarked),
            _ => source.Count(),
        };
    }

    private static IRenderable SavedQueryLabel(string name, int count = 0)
    {
        var countMarkup = count > 0 ? $"  [italic {ToHex(Amber)}]{count}[/]" : "";
        return new Markup($"[bold {ToHex(Gold)}]{Markup.Escape(name)}[/]{countMarkup}");
    }

    private static IRenderable FolderLabel(Folder folder)
    {
        var count = folder.UnreadCount > 0 ? $"  [italic {ToHex(Amber)}]{folder.UnreadCount}[/]" : "";
        return new Markup($"[bold #9370db]{Markup.Escape(folder.Name)}[/]{count}");
    }

    private static IRenderable FeedLabel(Feed feed)
    {
        var count = feed.UnreadCount > 0
            ? $"  [italic {ToHex(Amber)}]{feed.UnreadCount}[/]"
            : "  [italic grey50]0[/]";
        return new Markup($"{Markup.Escape(feed.Title)}{count}");
    }

    private static string ToHex(JColor c) => "#" + ((Spectre.Console.Color)c).ToHex();
}
