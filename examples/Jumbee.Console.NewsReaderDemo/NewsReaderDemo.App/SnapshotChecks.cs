using NewsReaderDemo.Core;
using System.Linq;
using Jumbee.Console;
using Jumbee.Console.Snapshot;

namespace NewsReaderDemo.App;

/// <summary>
/// Headless checks run via `dotnet run -- --test`, using Jumbee.Console.Snapshot against the app's real
/// wiring (NewsReaderDemoApp/ArticleListPanel/FeedTreeBuilder/ReaderPane/ArticleQuery) with a deterministic
/// in-memory library -- no network involved, so results are reproducible.
/// </summary>
/// <remarks>
/// <para>
/// Round 4: two of round 3's checks passed while the corresponding UI was visibly broken, because they asserted
/// model state (list counts, selected indices) instead of the RENDERED text. This round adds/adjusts checks that
/// assert what <see cref="ConsoleSnapshot.ToText"/> actually prints -- the "feed · age" column text and the
/// reader body's wrapped text -- so these two regressions (see the round-4 report) can't silently ship again.
/// </para>
/// <para>
/// Round 3 fix for a review finding (still true in round 4): every PNG in review/ is captured on its OWN
/// freshly-constructed, freshly-seeded <see cref="NewsReaderDemoApp"/>, AFTER the specific feature the caption claims
/// has been applied to THAT instance.
/// </para>
/// <para>
/// <b>Ordering constraint (unchanged in round 4)</b>: <c>UI.RegisterHotKey</c> is process-global static state, so
/// all <c>routeGlobal: true</c> checks/captures against one <see cref="NewsReaderDemoApp"/> instance are kept
/// CONTIGUOUS here, with no other instance constructed in between (see round-3 report for the full explanation).
/// </para>
/// </remarks>
public static class SnapshotChecks
{
    private const int W = 130, H = 40;
    private static int _pass, _fail;

    // The example's own review/ folder, resolved from the running assembly (NewsReaderDemo.App/bin/<cfg>/net10.0/)
    // rather than the current directory, so `--test` writes screenshots into the example regardless of where it's
    // launched from (e.g. `dotnet run --project ...` from the repo root).
    private static readonly string ReviewDir =
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "review"));

    private static string RPath(string fileName) => Path.Combine(ReviewDir, fileName);

    public static int RunAll()
    {
        Directory.CreateDirectory(ReviewDir);

        // ---- Phase 1: everything that exercises `app` via routeGlobal hotkeys, kept contiguous (see remarks). ----
        var app = NewSeededApp();

        Check("M0 layout renders folders/feeds/articles/reader",
            () =>
            {
                var text = ConsoleSnapshot.ToText(app.Root, W, H);
                return text.Contains("News") && text.Contains("BBC") && text.Contains("First Article Title")
                    && text.Contains("Feeds") && text.Contains("Articles");
            });

        Check("Tree shows the pinned saved queries + icons, connector-less (TreeGuide.None)",
            () =>
            {
                var text = ConsoleSnapshot.ToText(app.Root, W, H);
                // TreeGuide.None means no |/-/`+ connector glyphs anywhere in the tree pane's rendered text.
                var treePane = text.Split('\n').Where(l => l.Contains("Feeds") || l.Contains("News") || l.Contains("Tags")).ToArray();
                return text.Contains("Today Unread") && text.Contains("Today Marked") && text.Contains("All Feeds")
                    && !text.Contains("|--") && !text.Contains("`--");
            });

        Check("first article auto-selected on load (reader shows it without any key press)",
            () =>
            {
                var text = ConsoleSnapshot.ToText(app.Root, W, H);
                return app.ArticleList.SelectedArticle?.Title == "First Article Title"
                    && text.Contains("First Article Title"); // shown in the reader's title row too
            });

        SnapshotImage(app, RPath("01-main-view.png"));

        Check("M2 unread articles show the filled dot, read articles the hollow dot",
            () =>
            {
                var text = ConsoleSnapshot.ToText(app.Root, W, H);
                return text.Contains('●') && text.Contains('○');
            });

        Check("ROUND 4 FIX: the article row's RENDERED text actually shows the right-aligned 'feed · age' column " +
              "(round 3 computed it but the ListBox item probe-width/real-width mismatch silently dropped it -- " +
              "see ArticleRow remarks). Asserts the rendered text itself, not just that Build() produced a string.",
            () =>
            {
                var text = ConsoleSnapshot.ToText(app.Root, W, H);
                return text.Contains("BBC News · 1h") && text.Contains("Hacker News · 1d");
            });

        Check("M1 arrow-key navigation moves ListBox.SelectedIndex",
            () =>
            {
                var before = app.ArticleList.List.SelectedIndex;
                ConsoleSnapshot.ToTextAfter(app.ArticleList.List, W, H, ConsoleKey.DownArrow);
                return app.ArticleList.List.SelectedIndex == before + 1;
            });

        Check("M1 the global 'j' hotkey (routeGlobal, via UI.HotKeys.Char) forwards Down to the focused list",
            () =>
            {
                UI.SetFocus(app.ArticleList.List);
                app.ArticleList.List.SelectedIndex = 0;
                ConsoleSnapshot.ToTextAfter(app.ArticleList.List, W, H, [NewsReaderDemoApp.Key('j')], routeGlobal: true);
                return app.ArticleList.List.SelectedIndex == 1;
            });

        Check("ROUND 4 (Tree.SelectionChanged replaces raw-arrow global hotkeys): a raw Down arrow routed " +
              "straight to the focused tree updates the query scope via the new 0.1.4 event -- no global hotkey " +
              "registration needed for navigation keys any more",
            () => NavigateTreeToScope(app, "Today Unread"));

        Check("ROUND 3 FIX (single update flow / no state drift): marking the selected article read while " +
              "scoped to Unread makes it disappear from the visible list immediately -- the query is RE-APPLIED, " +
              "not just the row repainted",
            () =>
            {
                NavigateTreeToScope(app, "All Feeds");
                ConsoleSnapshot.ToTextAfter(app.ArticleList.List, W, H, [NewsReaderDemoApp.Key('2')], routeGlobal: true); // scope: Unread
                UI.SetFocus(app.ArticleList.List);
                app.ArticleList.List.SelectedIndex = 0;
                var target = app.ArticleList.SelectedArticle!;
                if (target.IsRead) throw new Exception("seed precondition violated: selected article already read");
                ConsoleSnapshot.ToTextAfter(app.ArticleList.List, W, H, [NewsReaderDemoApp.Key('r')], routeGlobal: true);
                var stillVisible = app.ArticleList.Articles.Contains(target);
                var ok = target.IsRead && !stillVisible;
                ConsoleSnapshot.ToTextAfter(app.ArticleList.List, W, H, [NewsReaderDemoApp.Key('1')], routeGlobal: true); // scope back to All
                return ok;
            });

        Check("selecting a feed leaf node in the tree filters the article list to that feed via ArticleQuery",
            () =>
            {
                var reached = NavigateTreeToScope(app, "BBC News");
                var after = app.ArticleList.Articles;
                var ok = reached && after.Count > 0 && after.All(a => a.Feed.Title == "BBC News");
                NavigateTreeToScope(app, "All Feeds"); // reset for subsequent checks
                return ok;
            });

        Check("scope hotkey '2' (unread) filters via ArticleQuery.ReadScope, '1' restores all",
            () =>
            {
                var totalBefore = app.ArticleList.Articles.Count;
                ConsoleSnapshot.ToTextAfter(app.ArticleList.List, W, H, [NewsReaderDemoApp.Key('2')], routeGlobal: true);
                var countAfter2 = app.ArticleList.Articles.Count;
                var unreadOnly = app.ArticleList.Articles.All(a => !a.IsRead) && countAfter2 > 0 && countAfter2 <= totalBefore;
                ConsoleSnapshot.ToTextAfter(app.ArticleList.List, W, H, [NewsReaderDemoApp.Key('1')], routeGlobal: true);
                var restored = app.ArticleList.Articles.Count == totalBefore;
                return unreadOnly && restored;
            });

        Check("M4 zen mode ('z') swaps DockPanel.FillControl to the bare reader and back (no split-collapse sliver)",
            () =>
            {
                app.ToggleZen();
                var zenText = ConsoleSnapshot.ToText(app.Root, W, H);
                var treeAndListHidden = !zenText.Contains("Today Unread") && !zenText.Contains("Hacker News") && !zenText.Contains("Second Article Title");
                app.ToggleZen();
                var restoredText = ConsoleSnapshot.ToText(app.Root, W, H);
                var restored = restoredText.Contains("Today Unread") && restoredText.Contains("Second Article Title");
                if (!(treeAndListHidden && restored))
                    throw new Exception($"treeAndListHidden={treeAndListHidden} restored={restored}");
                return true;
            });

        Check("M5 search (via ArticleQuery.SearchText) filters the article list to matching titles only",
            () =>
            {
                app.OpenSearch();
                app.SearchBox.Text = "Second";
                app.ApplySearch();
                var ok = app.ArticleList.Articles.Count == 1 && app.ArticleList.Articles[0].Title.Contains("Second");
                app.SearchBox.Text = "";
                app.ApplySearch();
                return ok;
            });

        Check("ROUND 4: empty-state message renders when a query matches nothing (not a silently blank list)",
            () =>
            {
                app.OpenSearch();
                app.SearchBox.Text = "zzz-no-such-article-zzz";
                app.ApplySearch();
                var text = ConsoleSnapshot.ToText(app.Root, W, H);
                var ok = app.ArticleList.Articles.Count == 0 && text.Contains("no articles match this view");
                app.SearchBox.Text = "";
                app.ApplySearch();
                return ok;
            });

        // `app` is done being driven via routeGlobal from here on -- everything below uses ITS OWN fresh,
        // exclusively-used instance (see the ordering-constraint remark on the class).

        Check("saved query 'Today Unread' scopes to (unread AND published in the last 24h) across the WHOLE library",
            () =>
            {
                var fresh = NewSeededApp();
                var reached = NavigateTreeToScope(fresh, "Today Unread");
                var after = fresh.ArticleList.Articles;
                // Seed data: article 1 is unread + 1h old (matches), article 2 is read (excluded), article 3 is
                // unread but 1 day old (excluded by the today predicate).
                return reached && after.Count == 1 && after[0].Title == "First Article Title";
            });

        Check("Round 2 regression guard: typing in the search box does NOT trigger the 'r' global hotkey (focus guard)",
            () =>
            {
                var guard = NewSeededApp();
                guard.OpenSearch();
                guard.ArticleList.List.SelectedIndex = 0;
                var article = guard.ArticleList.SelectedArticle!;
                article.IsRead = false;
                ConsoleSnapshot.ToTextAfter(guard.SearchBox, W, H, [NewsReaderDemoApp.Key('r')], routeGlobal: true);
                var stillUnread = !article.IsRead;
                var typedIntoBox = guard.SearchBox.Text.Contains('r');
                return stillUnread && typedIntoBox;
            });

        Check("ROUND 5: with plain MarkdownViewer (no consumer-level pre-wrap) on Jumbee.Console 0.1.4, the " +
              "reader body still WRAPS onto multiple lines in the narrow split-pane column (not clipped " +
              "mid-sentence). Asserts the full sentence's tail text is present in the rendered output.",
            () =>
            {
                var fresh = NewSeededApp();
                var text = ConsoleSnapshot.ToText(fresh.Root, W, H);
                return text.Contains("The full body of the first article,") && text.Contains("scroll through.");
            });

        Check("ROUND 4 FIX: the article row cache key includes Article.Age, so a stale cached row (read/marked/" +
              "flagged/width all unchanged) still re-renders once the age label itself would change",
            () =>
            {
                var fresh = NewSeededApp();
                var a = fresh.ArticleList.Articles[0];
                var before = ConsoleSnapshot.ToText(fresh.ArticleList.List, W, H);
                // Simulate time passing by moving the article's Published further into the past without
                // touching read/marked/flagged/width -- the OLD cache key (missing Age) would return the
                // stale cached segments; the fixed key must not.
                var older = a.Published!.Value.AddHours(-5);
                typeof(Article).GetProperty(nameof(Article.Published))!.SetValue(a, older);
                var after = ConsoleSnapshot.ToText(fresh.ArticleList.List, W, H);
                return before.Contains("BBC News · 1h") && after.Contains("BBC News · 6h") && !after.Contains("BBC News · 1h");
            });

        SnapshotScoped(RPath("05-scoped-feed.png"));
        SnapshotSavedQuery(RPath("06-saved-query.png"));
        SnapshotZen(RPath("02-zen-mode.png"));
        SnapshotSearch(RPath("03-search.png"));
        SnapshotReader(RPath("04-reader-detail.png"));
        SnapshotEmpty(RPath("07-empty-state.png"));
        SnapshotSyncing(RPath("08-syncing.png"));

        System.Console.WriteLine($"\n{_pass} passed, {_fail} failed");
        return _fail == 0 ? 0 : 1;
    }

    private static void Check(string name, Func<bool> assertion)
    {
        bool ok;
        string? error = null;
        try { ok = assertion(); }
        catch (Exception ex) { ok = false; error = ex.Message; }
        if (ok) { _pass++; System.Console.WriteLine($"PASS  {name}"); }
        else { _fail++; System.Console.WriteLine($"FAIL  {name}{(error is null ? "" : $"  ({error})")}"); }
    }

    /// <summary>Rewinds the tree to its top (Up-arrow spam, routed exactly as a user would), then presses Down
    /// until the status bar reports <paramref name="scopeLabel"/> as the active scope. Idempotent and
    /// order-independent. Round 4: the tree no longer needs arrow keys registered as global hotkeys (see class
    /// remarks) -- <c>Tree.SelectionChanged</c> fires from ordinary control-routed input, so <c>routeGlobal</c>
    /// here now falls through to plain routing (harmless to keep). MUST be the most-recently-constructed
    /// <see cref="NewsReaderDemoApp"/> in the process while any OTHER routeGlobal key ('j','r','1','2',...) is still
    /// in play (see class remarks on global hotkeys).</summary>
    private static bool NavigateTreeToScope(NewsReaderDemoApp app, string scopeLabel)
    {
        UI.SetFocus(app.FeedTree);
        ConsoleKeyInfo up = new('\0', ConsoleKey.UpArrow, false, false, false);
        ConsoleKeyInfo down = new('\0', ConsoleKey.DownArrow, false, false, false);
        for (var i = 0; i < 15; i++)
            ConsoleSnapshot.ToTextAfter(app.FeedTree, W, H, [up], routeGlobal: true);
        for (var i = 0; i < 15; i++)
        {
            ConsoleSnapshot.ToTextAfter(app.FeedTree, W, H, [down], routeGlobal: true);
            if (app.StatusBar.Text.Contains(scopeLabel)) return true;
        }
        return false;
    }

    private static NewsReaderDemoApp NewSeededApp()
    {
        var app = new NewsReaderDemoApp();
        app.OnLibraryLoaded(SeedLibrary());
        return app;
    }

    private static void SnapshotImage(NewsReaderDemoApp app, string path) =>
        ConsoleSnapshot.SavePng(app.Root, W, H, path);

    private static void SnapshotZen(string path)
    {
        var fresh = NewSeededApp();
        fresh.ToggleZen();
        ConsoleSnapshot.SavePng(fresh.Root, W, H, path);
    }

    private static void SnapshotSearch(string path)
    {
        var fresh = NewSeededApp();
        fresh.OpenSearch();
        fresh.SearchBox.Text = "Second";
        fresh.ApplySearch();
        ConsoleSnapshot.SavePng(fresh.Root, W, H, path);
    }

    private static void SnapshotReader(string path)
    {
        var fresh = NewSeededApp();
        fresh.ArticleList.List.SelectedIndex = 0;
        ConsoleSnapshot.SavePng(fresh.Root, W, H, path);
    }

    private static void SnapshotScoped(string path)
    {
        var fresh = NewSeededApp();
        NavigateTreeToScope(fresh, "BBC News");
        ConsoleSnapshot.SavePng(fresh.Root, W, H, path);
    }

    private static void SnapshotSavedQuery(string path)
    {
        var fresh = NewSeededApp();
        NavigateTreeToScope(fresh, "Today Unread");
        ConsoleSnapshot.SavePng(fresh.Root, W, H, path);
    }

    private static void SnapshotEmpty(string path)
    {
        var fresh = NewSeededApp();
        fresh.OpenSearch();
        fresh.SearchBox.Text = "zzz-no-such-article-zzz";
        fresh.ApplySearch();
        ConsoleSnapshot.SavePng(fresh.Root, W, H, path);
    }

    private static void SnapshotSyncing(string path)
    {
        var fresh = new NewsReaderDemoApp(); // never OnLibraryLoaded -- still in the initial "syncing" state
        ConsoleSnapshot.SavePng(fresh.Root, W, H, path);
    }

    private static Library SeedLibrary()
    {
        var bbc = new Feed { Title = "BBC News", Url = "https://example.test/bbc" };
        var hn = new Feed { Title = "Hacker News", Url = "https://example.test/hn" };
        var now = DateTimeOffset.UtcNow;
        bbc.Articles.AddRange(
        [
            new Article
            {
                Id = "1", Title = "First Article Title", Feed = bbc, Published = now.AddHours(-1),
                Tags = ["breaking"], Author = "J. Reporter",
                Body = "The full body of the first article, several sentences long so the reader pane has real content to scroll through.",
                Link = "https://example.test/first",
            },
            new Article
            {
                Id = "2", Title = "Second Article Title", Feed = bbc, Published = now.AddHours(-3), IsRead = true,
                Body = "Body of the second article.", Link = "https://example.test/second",
            },
        ]);
        hn.Articles.AddRange(
        [
            new Article
            {
                Id = "3", Title = "Third Article Title", Feed = hn, Published = now.AddDays(-1),
                Tags = ["tech"], Body = "Body of the third article.", Link = "https://example.test/third",
            },
        ]);
        return new Library { Folders = [new Folder { Name = "News", Feeds = [bbc, hn] }] };
    }
}
