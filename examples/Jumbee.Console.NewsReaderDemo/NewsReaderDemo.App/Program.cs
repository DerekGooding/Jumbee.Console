using NewsReaderDemo.App.Controls;
using NewsReaderDemo.Core;
using Jumbee.Console;
using JTree = Jumbee.Console.Tree;

namespace NewsReaderDemo.App;

public class Program
{
    public static async Task<int> Main(string[] args)
    {
        if (args.Contains("--test"))
            return SnapshotChecks.RunAll();

        var app = new NewsReaderDemoApp();
        // A CancellationTokenSource tied to UI.Start's own lifetime means Ctrl+C/'q' during an in-flight
        // fetch cancels FeedReader's HTTP calls instead of letting a background Task.Run outlive the UI loop
        // it was posting back into (round 2's startup fetch had no cancellation at all).
        using var startupCts = new CancellationTokenSource();
        var uiTask = UI.Start(app.Root, width: 130, height: 40, input: new VtInputSource(anyMotion: true));

        // Fetch/parse all feeds off the UI thread; marshal the populated Library back via UI.Post so the
        // startup render is never blocked on network I/O. A failed fetch (offline, DNS, timeout, ...) reports
        // through the status bar instead of leaving the app silently blank forever (round 4 fix).
        _ = Task.Run(async () =>
        {
            try
            {
                var library = await new FeedService().LoadAsync(Subscriptions.Default, startupCts.Token);
                UI.Post(() => app.OnLibraryLoaded(library));
            }
            catch (OperationCanceledException) { /* app closed before the fetch finished */ }
            catch (Exception ex)
            {
                UI.Post(() => app.OnLibraryLoadFailed(ex));
            }
        }, startupCts.Token);

        try { await uiTask; }
        finally { startupCts.Cancel(); }
        return 0;
    }
}

public static class Subscriptions
{
    public static readonly FeedSubscription[] Default =
    [
        new("News", "BBC News", "https://feeds.bbci.co.uk/news/rss.xml"),
        new("News", "NYT Home", "https://rss.nytimes.com/services/xml/rss/nyt/HomePage.xml"),
        new("Tech", "Hacker News", "https://hnrss.org/frontpage"),
        new("Tech", "Ars Technica", "https://feeds.arstechnica.com/arstechnica/index"),
    ];
}

/// <summary>
/// Owns the eilmeldung UI tree and all interaction wiring (hotkeys, focus cycling, zen mode, search, scope).
/// Domain state (<see cref="Library"/>) lives in NewsReaderDemo.Core; the tree-to-list filtering predicate itself
/// lives in <see cref="ArticleQuery"/> (Core), not here -- this class only ever asks Core for "the current set"
/// and hands it to <see cref="ArticleListPanel"/>.
/// </summary>
/// <remarks>
/// <para>
/// <b>Round 4 — <see cref="JTree.SelectionChanged"/> replaces the raw-arrow-key global-hotkey hack.</b> 0.1.4
/// added <c>Tree.SelectionChanged</c> ("raised whenever the highlighted node changes (arrow/vim keys, Home/End/
/// PageUp/PageDown, or a mouse click) ... use it to follow tree navigation instead of only reacting to
/// <c>NodeActivated</c>"). Round 3 had no such event, so it registered the raw arrow keys themselves as GLOBAL
/// hotkeys (<c>RegisterNavKey</c>) purely to notice "the tree highlight moved" and re-derive the scope — a
/// workaround that also mis-fired for Home/End/PageUp/PageDown (never hooked) and mouse clicks (not a key at
/// all). <see cref="SyncTreeSelection"/> now runs off <c>FeedTree.SelectionChanged</c> directly, covers every
/// documented way the selection can move, and the app no longer needs to register arrow keys globally at all —
/// arrows reach the focused tree the normal (non-hotkey) input path.
/// </para>
/// <para>
/// <b>Round 4 — <see cref="UI.HotKeys.Char(char)"/> replaces the hand-built <see cref="ConsoleKeyInfo"/>.</b> The
/// 0.1.4 changelog flags exactly the bug round 3's <c>Key(char)</c> helper had: it built punctuation keys (the
/// <c>/</c> search hotkey) with an assumed <see cref="ConsoleKey"/> derived from <c>char.ToUpperInvariant('/')</c>
/// — which does not match what the real input decoder produces for a <c>/</c> keypress (key code <c>0</c> +
/// character, per <c>UI.HotKeys.Char</c>'s remarks: "every non-letter/digit ... uses key code 0 with the
/// character"). <see cref="Register"/> now calls <c>UI.HotKeys.Char</c> for every registration AND every
/// simulated keypress in <c>SnapshotChecks</c>, so the hotkey and the test both go through the one function that
/// mirrors the real input decoder.
/// </para>
/// Round 3's single state-mutation flow is unchanged: EVERY article state change (read/mark) goes through
/// <see cref="MutateArticle"/>, which mutates the article, refreshes the tree's unread counts, then
/// re-applies the active <see cref="ArticleQuery"/> via <see cref="ApplyQuery"/>.
/// </remarks>
public class NewsReaderDemoApp
{
    private readonly FeedTreeBuilder _treeBuilder = new();
    private static BorderStyle borderStyle = BorderStyle.Heavy;
    private static Color borderFgColor = Color.Magenta1;
    private static TitleStyle titleStyle = new TitleStyle(TitlePos.TopLeft, borderStyle: TitleBorderStyle.Inline, TitleColorStyle.Reverse);
    public JTree FeedTree => _treeBuilder.Tree;
    public ArticleListPanel ArticleList { get; } = new();
    public ReaderPane Reader { get; } = new();
    public TextLabel StatusBar { get; }
    public TextInput SearchBox { get; }
    public DockPanel Root { get; }

    private readonly IFocusable _readerFramed;
    private readonly IFocusable _outerSplit;
    private readonly ArticleQuery _query = new();
    private Library _library = new();
    private TreeScope? _lastTreeScope;
    private bool _zen;
    private SyncState _sync = SyncState.Syncing;
    private string? _syncError;

    private enum SyncState { Syncing, Loaded, Failed }

    public NewsReaderDemoApp()
    {
        StatusBar = new TextLabel(TextLabelOrientation.Horizontal, "", Color.Grey);
        SearchBox = new TextInput(placeholder: "search articles...");

        // Bright amber full-row selection (round 2's dark-brown background read as muddy, not amber) plus a
        // purple tree selection so the two panes stay visually distinct.
        Color selectionBg = Spectre.Console.Color.FromHex("#ffb000");
        Color selectionFg = Spectre.Console.Color.Black;
        ArticleList.List.SelectedBackgroundColor = selectionBg;
        ArticleList.List.SelectedForegroundColor = selectionFg;
        FeedTree.SelectedBackgroundColor = Spectre.Console.Color.FromHex("#4b0082");
        FeedTree.SelectedForegroundColor = Spectre.Console.Color.FromHex("#d8bfff");

        var treeFramed = FeedTree.WithBorder(borderStyle).WithTitle("Feeds", titleStyle);
        var listFramed = ArticleList.List.WithBorder(borderStyle).WithTitle("Articles", titleStyle);
        _readerFramed = Reader.WithBorder(borderStyle).WithTitle("Article", titleStyle);

        var innerSplit = new SplitPanel(SplitOrientation.Vertical, listFramed, _readerFramed, 12) { MinFirst = 0 };
        _outerSplit = new SplitPanel(SplitOrientation.Horizontal, treeFramed, innerSplit, 32) { MinFirst = 0 };
        Root = new DockPanel(DockedControlPlacement.Bottom, StatusBar.WithHeight(1), _outerSplit);

        ArticleList.SelectionChanged += (_, article) => Reader.SetArticle(article);
        FeedTree.SelectionChanged += (_, _) => SyncTreeSelection();
        FeedTree.NodeActivated += (_, _) => SyncTreeSelection();
        SearchBox.Submitted += (_, _) => ApplySearch();

        RegisterHotKeys();
        UpdateStatusBar();
        UI.SetFocus(ArticleList.List);
    }

    public void OnLibraryLoaded(Library library)
    {
        _library = library;
        _sync = SyncState.Loaded;
        _syncError = null;
        // A node the previous library's tree held onto (via node Tag) would be stale after a reload, since
        // Populate rebuilds every node -- reset scope back to the library root so SyncTreeSelection never
        // compares a fresh selection against a TreeScope that references a Folder/Feed object from the OLD
        // library instance.
        _query.TreeScope = TreeScope.Library;
        _lastTreeScope = null;
        _treeBuilder.Populate(library);
        ApplyQuery();
    }

    /// <summary>Reports a failed startup fetch (offline, DNS, timeout, ...) on the status bar instead of leaving
    /// the app silently blank forever -- round 4 fix for the "no loading/empty/offline state" finding.</summary>
    public void OnLibraryLoadFailed(Exception ex)
    {
        _sync = SyncState.Failed;
        _syncError = ex.Message;
        UpdateStatusBar();
    }

    #region Query / scope

    private void ApplyQuery(Article? preferredSelection = null)
    {
        ArticleList.Populate(_query.Apply(_library), preferredSelection);
        UpdateStatusBar();
    }

    private void UpdateStatusBar()
    {
        var prefix = _sync switch
        {
            SyncState.Syncing => "syncing feeds...  |  ",
            SyncState.Failed => $"offline -- feed sync failed ({_syncError})  |  ",
            _ => "",
        };
        var countPart = ArticleList.Articles.Count == 0
            ? "no articles match this view"
            : $"{ArticleList.Articles.Count} articles";
        StatusBar.Text = $"{prefix}{_query.Describe()}  |  {countPart}  |  " +
                          "j/k move  h/l panel  o open  r/u read  m mark  1/2/3 scope  z zen  / search  q quit";
    }

    private void SyncTreeSelection()
    {
        if (_treeBuilder.CurrentScope() is not { } scope || scope.Equals(_lastTreeScope)) return;
        _lastTreeScope = scope;
        _query.TreeScope = scope;
        ApplyQuery();
    }

    private void SetReadScope(ReadScope scope)
    {
        _query.ReadScope = scope;
        ApplyQuery();
    }

    /// <summary>The single path for every read/mark/flag state change: mutate the article, refresh the tree's
    /// unread counts, then re-apply the active query so the visible list can never drift from what the current
    /// scope says should be there (see class remarks).</summary>
    private void MutateArticle(Action<Article> mutate)
    {
        if (ArticleList.SelectedArticle is not { } a) return;
        mutate(a);
        _treeBuilder.RefreshCounts();
        ApplyQuery(preferredSelection: a);
    }

    #endregion

    #region Hotkeys

    // UI.RegisterHotKey's own doc says a hotkey is "handled before any control (so it works regardless of
    // focus)", and UI.SendInput's doc confirms the same for routeGlobal: a matched hotkey "marks the event
    // handled, in which case the focused control never sees the key" -- BEFORE the registered delegate even
    // runs. So a delegate-level focus guard does NOT help: the keystroke is swallowed at the dispatch level
    // regardless of what the delegate does. The only documented way to let a bare letter reach a focused control
    // is to remove the conflicting hotkeys via UI.UnregisterHotKey while that control is focused, and re-register
    // them when it loses focus -- there is no "local"/scoped hotkey concept in the public API (still true in
    // 0.1.4: the changelog documents the hotkey table as process-global, not per-instance/-focus-scoped).
    private readonly Dictionary<ConsoleKeyInfo, Action> _letterHotkeys = [];

    private void RegisterHotKeys()
    {
        UI.RegisterHotKey(UI.HotKeys.Escape, HandleEscape);
        Register('q', UI.Stop);
        Register('j', () => Forward(ConsoleKey.DownArrow));
        Register('k', () => Forward(ConsoleKey.UpArrow));
        Register('h', () => CyclePanel(-1));
        Register('l', () => CyclePanel(+1));
        Register('o', OpenSelectedInBrowser);
        Register('r', () => MarkRead(true));
        Register('u', () => MarkRead(false));
        Register('m', ToggleMarked);
        Register('z', ToggleZen);
        Register('/', OpenSearch);
        Register('1', () => SetReadScope(ReadScope.All));
        Register('2', () => SetReadScope(ReadScope.Unread));
        Register('3', () => SetReadScope(ReadScope.Marked));
    }

    private void Register(char c, Action action)
    {
        var key = UI.HotKeys.Char(c);
        _letterHotkeys[key] = action;
        UI.RegisterHotKey(key, action);
    }

    /// <summary>Same key <see cref="Register"/> uses -- exposed so <c>SnapshotChecks</c> simulates the exact
    /// value a registered hotkey compares equal to (see class remarks on <see cref="UI.HotKeys.Char(char)"/>).</summary>
    internal static ConsoleKeyInfo Key(char c) => UI.HotKeys.Char(c);

    private void SuspendLetterHotkeys()
    {
        foreach (var key in _letterHotkeys.Keys) UI.UnregisterHotKey(key);
    }

    private void ResumeLetterHotkeys()
    {
        foreach (var (key, action) in _letterHotkeys) UI.RegisterHotKey(key, action);
    }

    private void Forward(ConsoleKey key)
    {
        if (UI.Focused is IFocusable f)
            UI.SendInput(f, new ConsoleKeyInfo('\0', key, false, false, false));
    }

    private void CyclePanel(int direction)
    {
        IFocusable[] order = [FeedTree, ArticleList.List, Reader];
        var i = Array.IndexOf(order, UI.Focused);
        var next = order[Math.Clamp((i < 0 ? 0 : i) + direction, 0, order.Length - 1)];
        UI.SetFocus(next);
    }

    private void MarkRead(bool read) => MutateArticle(a => a.IsRead = read);

    private void ToggleMarked() => MutateArticle(a => a.IsMarked = !a.IsMarked);

    private void OpenSelectedInBrowser()
    {
        if (ArticleList.SelectedArticle is not { Link.Length: > 0 } a) return;
        try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(a.Link) { UseShellExecute = true }); }
        catch { /* no browser available in this environment; non-fatal */ }
        MarkRead(true);
    }

    public bool IsZen => _zen;

    /// <summary>Toggles full-screen reader ("zen") mode by reassigning <see cref="DockPanel.FillControl"/> between
    /// the 3-pane split and the bare reader -- the technique the public <c>UI.Layout</c> XML remarks call out
    /// ("reassign a DockPanel.DockedControl / FillControl") as the alternative to
    /// <see cref="SplitPanel.SplitPosition"/> collapse.</summary>
    public void ToggleZen()
    {
        _zen = !_zen;
        Root.FillControl = _zen ? _readerFramed : _outerSplit;
        UI.SetFocus(_zen ? Reader : ArticleList.List);
    }

    private void HandleEscape()
    {
        if (ReferenceEquals(UI.Focused, SearchBox))
            CloseSearch(apply: false);
        else
            UI.Stop();
    }

    public void OpenSearch()
    {
        SearchBox.Text = _query.SearchText;
        Root.DockedControl = SearchBox.WithHeight(1);
        SuspendLetterHotkeys();
        UI.SetFocus(SearchBox);
    }

    public void ApplySearch() => CloseSearch(apply: true);

    private void CloseSearch(bool apply)
    {
        if (apply)
        {
            _query.SearchText = SearchBox.Text.Trim();
            ApplyQuery();
        }
        Root.DockedControl = StatusBar.WithHeight(1);
        ResumeLetterHotkeys();
        UI.SetFocus(ArticleList.List);
    }

    #endregion
}
