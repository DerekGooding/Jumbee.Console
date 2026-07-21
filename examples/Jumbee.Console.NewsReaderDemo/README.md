# Porting the *eilmeldung* RSS reader (AI-generated)

![The finished newsreader: a feed tree on the left, an article list top-right, and a markdown reader filling the bottom](https://i.imgur.com/5waCHVR.gif)

> Runnable example. Build and run the headless snapshot suite from the repo root with
> `dotnet run --project examples/Jumbee.Console.NewsReaderDemo/NewsReaderDemo.App -c Release -- --test`.

I wanted to see how far a newcomer could get with Jumbee.Console beyond the two-pane hello-world in the getting
started doc, so I picked a real app to port: [eilmeldung](https://github.com/christo-auer/eilmeldung), a Rust/ratatui terminal RSS
reader with a distinctive look — a colored folder/feed tree on the left, an article list top-right with
read/unread dots and tag pills, and a big article reader dominating the bottom-right. Not a toy. A real app with
real state, real async I/O, and a UI that has to *feel* like something, not just compile.

This is the story of building it: the concepts I leaned on, the mistakes I made, and the couple of places where
I had to bend my approach around how the library does things. If you're starting your own Jumbee.Console app, I
hope this saves you some of the same detours.

Here's the finished thing (`review/01-main-view.png`): a feed tree on the left, the article list top-right with
● unread / ○ read dots and a right-aligned "feed · age" column, and the reader pane along the bottom taking up
most of the screen — deliberately. In eilmeldung the reader is the dominant region, not an afterthought, and
matching that proportion was the very first decision I made.

## The mental model: a tree of controls, one thread, properties trigger repaint

If you've built a WinForms or WPF app, Jumbee.Console will feel oddly familiar. You build a tree of `Control`
objects, arrange them with a layout, and set properties on them — `myLabel.Text = "hello"` — and the control
repaints itself. There's no manual "now redraw" step to remember, because setting a property invalidates the
control and the next frame picks up the change.

The other half of the model, and the one thing to internalize before you write a line of async code: **everything
UI-related happens on one dedicated UI thread.** There's no lock to reach for and no `InvokeRequired` dance —
just one rule: if you're not already on that thread, marshal onto it. `UI.Invoke`, `UI.Post`, and
`UI.InvokeAsync` are the three ways in; the difference is whether you block for the result (`Invoke`), fire and
forget (`Post`), or await it (`InvokeAsync`). I'll come back to this in a minute because it's the single most
important thing I got wrong on my first pass through this port.

## What I'm building, and how I split it up

eilmeldung's own docs (`docs/getting-started.md`, `docs/keybindings.md`, `docs/queries.md` in the reference repo)
describe a folder/feed/tag tree, a filterable article list, a reader with a metadata header, saved queries like
"Today Unread", vim-style navigation, a zen/full-screen mode, and marking/flagging. That's a lot of surface area,
and none of it is Jumbee.Console's problem — it's *my* domain logic. So before touching a single control I split
the solution in two:

- **`NewsReaderDemo.Core`** — plain C#, no UI reference at all. `Article`, `Feed`, `Folder`, `Library` are the
  domain models; `ArticleQuery` is the filter/scope logic (all-feeds vs. one folder vs. a saved query, `unread`/
  `marked`/free-text search); `FeedService` wraps `CodeHollow.FeedReader` for the actual fetch/parse.
- **`NewsReaderDemo.App`** — the TUI. It never builds a filter predicate or reaches into `FeedReader` results
  directly; it asks Core for "the current set" and hands it to a control.

This paid off immediately: `ArticleQuery.Apply(Library)` is pure data-in, data-out, so I could unit-test the
saved-query logic ("Today Unread" = unread AND published in the last 24 hours, across the *whole* library
regardless of what folder was previously selected) without spinning up any UI at all. Here's the shape of it — a
`TreeScope` (all / a folder / a feed / a tag / a saved query) narrowed by read state and search text:

```csharp
public IEnumerable<Article> Apply(Library library)
{
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
    // ... folder / feed / tag / library scopes, then ReadScope, then search ...
}
```

The app layer's job is just: run this against the library, hand the result to the list control, done.

## Fetching feeds off the UI thread (and the mistake I made the first time)

RSS fetching is network I/O — it can take seconds, and it must never block the render loop. `CodeHollow.FeedReader`
gives you `FeedReader.ReadAsync`, so the natural shape is a background `Task` that fetches everything and hands
the finished `Library` back to the UI thread:

```csharp
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
```

`UI.Post` is the fire-and-forget marshal — the fetch thread doesn't wait for the UI to apply the result, it just
queues the work and moves on. `OnLibraryLoaded` then does all its control mutation — populating the tree,
populating the list — safely on the UI thread, because that's the thread `Post`'s callback runs on.

The mistake, from an earlier pass at this port: I didn't tie the fetch to anything, so hitting `q` to quit while
a fetch was still in flight left an orphaned `Task.Run` happily finishing its HTTP call into a UI that no longer
existed. The fix is boring, and the kind of thing you only learn by getting bitten once — a
`CancellationTokenSource` scoped to the same lifetime as `UI.Start`, passed all the way into
`FeedReader.ReadAsync`:

```csharp
using var startupCts = new CancellationTokenSource();
var uiTask = UI.Start(app.Root, width: 130, height: 40, input: new VtInputSource(anyMotion: true));
// ... Task.Run(..., startupCts.Token) ...
try { await uiTask; }
finally { startupCts.Cancel(); }
```

This is the concept I'd tell any newcomer to get right early: figure out *which* thread you're on before you
touch a control, and if you're not on the UI thread, `Invoke`/`Post`/`InvokeAsync` your way there. Everything
else about background work in a Jumbee.Console app follows from that one rule.

## Laying out the three-region shell

eilmeldung's layout is a left tree, a top-right list, and a big bottom-right reader. Jumbee.Console builds this
out of nested `SplitPanel`s — pick an orientation, give it two controls and a split position:

```csharp
var innerSplit = new SplitPanel(SplitOrientation.Vertical, listFramed, _readerFramed, 12) { MinFirst = 0 };
_outerSplit = new SplitPanel(SplitOrientation.Horizontal, treeFramed, innerSplit, 32) { MinFirst = 0 };
Root = new DockPanel(DockedControlPlacement.Bottom, StatusBar.WithHeight(1), _outerSplit);
```

Reading it inside-out: the inner split stacks the article list over the reader with the list pinned to 12 rows
(`MinFirst = 0` lets it collapse further rather than refusing to shrink), so the reader gets everything else —
exactly the "reader as dominant region" proportion eilmeldung has. The outer split puts the feed tree at a fixed
32 columns next to that. A `DockPanel` docks the status bar to the bottom and lets the split fill the rest.

`.WithBorder(BorderStyle.Rounded).WithTitle("Feeds")` is a nice one to know early — every control gets a
one-line way to wrap itself in a bordered, titled frame without hand-building a border control.

Zen mode (a full-screen reader with one keystroke) turned out to be the same `DockPanel` doing double duty —
just reassign what it's currently filling:

```csharp
public void ToggleZen()
{
    _zen = !_zen;
    Root.FillControl = _zen ? _readerFramed : _outerSplit;
    UI.SetFocus(_zen ? Reader : ArticleList.List);
}
```

No special "zen mode" primitive needed — `DockPanel.FillControl` is just a property, and reassigning it is the
same repaint mechanism as setting `Text` on a label. See `review/02-zen-mode.png`: same reader content, tree and
list simply gone, one property flip.

## The building blocks: Tree, ListBox, MarkdownViewer — and where I needed my own controls

For the three regions I used three built-in controls almost as-is:

- **`Tree`** for the feed/folder/tag/saved-query sidebar. Each `TreeNode` gets a `Label` (any `IRenderable`, so
  I used colored `Markup` for folder/feed names), a `LeafGlyph`/`LeafGlyphColor` for its icon, and — the thing
  that saved me a side-dictionary — a `Tag` slot for arbitrary application data. I stash the node's `TreeScope`
  (all-feeds / this folder / this feed / this tag / this saved query) directly on the node:

  ```csharp
  var todayUnread = Tree.Root.AddChild(SavedQueryLabel("Today Unread"), "Today Unread");
  todayUnread.Tag = TreeScope.ForSavedQuery("Today Unread", ReadScope.Unread, today: true);
  ```

  and later `Tree.SelectedNode?.Tag as TreeScope?` gets me straight back to the domain object — no parallel list
  to keep in sync.

- **`ListBox`** for the article list, same `Tag` trick for the selected `Article`.

- **`MarkdownViewer`** for the reader body, since article content is free-form text and I wanted real
  markdown rendering (links, emphasis) rather than a plain label.

None of those gives you an *article row* with a read/unread dot, a title, tag pills, and a right-aligned
"feed · age" column, though — that's eilmeldung's look, not a stock control's. For that I wrote my own
`ArticleRow`, a small class implementing Spectre.Console's `IRenderable` directly (the same interface
Jumbee.Console's own text controls render through), so a `ListBox` item can be *any* renderable, not just a
string:

```csharp
public IEnumerable<Segment> Render(RenderOptions options, int maxWidth)
{
    var key = (article.IsRead, article.IsMarked, article.IsFlagged, article.Age, effectiveWidth);
    if (_cachedSegments is null || key != _cacheKey)
    {
        _cachedSegments = [.. ((IRenderable)Build(article, effectiveWidth)).Render(options, effectiveWidth)];
        _cacheKey = key;
    }
    return _cachedSegments;
}
```

`Build` composes the dot, flag/star, title, tag pills, and right column into one `Markup` string, measuring
everything with `GetCellWidth` (not `.Length`) so wide glyphs don't throw off the column math. Similarly,
`ReaderPane` is a `CompositeControl` — several child controls (title, meta line, tags, image placeholder, body)
stacked in a `DockPanel` and handed to `SetContent` once in the constructor — because the reader's structured
metadata header is its own little layout problem, not something you'd rebuild from scratch every time an article
changes.

Building `ArticleRow` also taught me a real lesson about the boundary between "measure" and "actual on-screen
width": `ListBox` calls an item's `Render` at a generous internal probe width rather than the column's real
width, so I clamp against the owning `ListBox`'s own `ActualWidth` (a documented property — "the control's
actual laid-out width in cells") instead of trusting the width `Render` hands me directly. Once I did that, the
right-aligned "feed · age" column showed up exactly where it should (`review/01-main-view.png`, rightmost text on
each row).

One thing that got easier as the library matured under me: `MarkdownViewer` used to need a hand-rolled word-wrap
before I fed it article bodies, because plain paragraph text wasn't reflowing to the control's width on its own.
That's fixed now — `ReaderPane` hands `article.Body` straight to `_body.Markdown` with no pre-processing:

```csharp
private void RenderBody()
{
    if (_current is not { } article) { _body.Markdown = "*(no article selected)*"; return; }
    _body.Markdown = string.IsNullOrWhiteSpace(article.Body) ? "*(no content)*" : article.Body;
}
```

and the reader body still wraps correctly onto multiple lines in a ~95-cell column (verified below) — one less
thing to think about, which is exactly the direction you want a library to move as you build against it.

## Vim-style keys with global hotkeys

eilmeldung's keybindings are vim-flavored: `j`/`k` to move, `o` to open, `r`/`u` to mark read/unread, `/` to
search, `q` to quit, `1`/`2`/`3` to switch scope. `UI.RegisterHotKey` is the mechanism — register a key against
an `Action`, and it fires "regardless of focus," which is what you want for a single-letter navigation key that
should work no matter which pane has focus:

```csharp
private void Register(char c, Action action)
{
    var key = UI.HotKeys.Char(c);
    _letterHotkeys[key] = action;
    UI.RegisterHotKey(key, action);
}
```

`UI.HotKeys.Char(c)` builds the right `ConsoleKeyInfo` for a bare character — I initially hand-built these myself
and got punctuation wrong (`/` doesn't map the way you'd assume from `char.ToUpperInvariant`), so leaning on the
library's own helper instead of rolling my own was the right call once it existed.

The one sharp edge worth flagging for a newcomer: because a global hotkey wins *before* any control sees the key,
typing `r` into the search box would otherwise "mark read" instead of typing the letter. The fix is to suspend
the letter hotkeys while the search box is focused and resume them when it closes:

```csharp
public void OpenSearch()
{
    SearchBox.Text = _query.SearchText;
    Root.DockedControl = SearchBox.WithHeight(1);
    SuspendLetterHotkeys();
    UI.SetFocus(SearchBox);
}
```

See `review/03-search.png` for the search box docked in over the status bar. The search itself is just
`ArticleQuery.SearchText` re-applied through the same `Apply(Library)` path everything else goes through — title,
summary, author, and feed name are all matched.

## Testing a TUI without a terminal

You can't screenshot a keypress, and you can't hand a reviewer a live terminal session. This is exactly what
`Jumbee.Console.Snapshot` is for: it renders a control tree to plain text (or a PNG) without opening a real
console, and it can feed simulated keys through the same input path a real terminal would use.

The pattern I ended up using everywhere: build the app, seed it with deterministic in-memory data (no network, so
results are reproducible), then assert on the *rendered text* — not just on model state:

```csharp
Check("M2 unread articles show the filled dot, read articles the hollow dot",
    () =>
    {
        var text = ConsoleSnapshot.ToText(app.Root, W, H);
        return text.Contains('●') && text.Contains('○');
    });
```

That last clause — asserting rendered text instead of a model flag like `article.IsRead` — caught a real
regression for me. Early on, a check asserted "the row's `Build()` method produces a string containing the feed
name and age," and it passed, while the actual on-screen list silently dropped that column entirely (the
`ListBox` probe-width issue from the previous section). The row *was* correct; the pixels weren't. Once I
switched every check to `ConsoleSnapshot.ToText(app.Root, ...).Contains(...)` — the same text a person looking at
the terminal would see — that class of "passes but looks broken" bug stopped being possible.

For key-driven behavior, `ConsoleSnapshot.ToTextAfter` sends simulated keys to a focused control and re-renders:

```csharp
Check("M1 arrow-key navigation moves ListBox.SelectedIndex",
    () =>
    {
        var before = app.ArticleList.List.SelectedIndex;
        ConsoleSnapshot.ToTextAfter(app.ArticleList.List, W, H, ConsoleKey.DownArrow);
        return app.ArticleList.List.SelectedIndex == before + 1;
    });
```

and for global hotkeys specifically, a `routeGlobal: true` flag makes the simulated key actually run through
`UI.RegisterHotKey`'s dispatch instead of just handing it straight to one control:

```csharp
Check("M1 the global 'j' hotkey (routeGlobal, via UI.HotKeys.Char) forwards Down to the focused list",
    () =>
    {
        UI.SetFocus(app.ArticleList.List);
        app.ArticleList.List.SelectedIndex = 0;
        ConsoleSnapshot.ToTextAfter(app.ArticleList.List, W, H, [NewsReaderDemoApp.Key('j')], routeGlobal: true);
        return app.ArticleList.List.SelectedIndex == 1;
    });
```

The important detail there: build the simulated key with the *same* helper you used to register it
(`UI.HotKeys.Char('j')`), not your own guess at a `ConsoleKeyInfo` — otherwise the simulated key won't match what's
registered and the check will silently do nothing.

Running the whole suite is just `dotnet run --project examples/Jumbee.Console.NewsReaderDemo/NewsReaderDemo.App -c Release -- --test` — no terminal, no window, works in CI:

```
PASS  M0 layout renders folders/feeds/articles/reader
PASS  M2 unread articles show the filled dot, read articles the hollow dot
PASS  M1 the global 'j' hotkey (routeGlobal, via UI.HotKeys.Char) forwards Down to the focused list
PASS  M4 zen mode ('z') swaps DockPanel.FillControl to the bare reader and back (no split-collapse sliver)
PASS  M5 search (via ArticleQuery.SearchText) filters the article list to matching titles only
...
18 passed, 0 failed
```

And for the review package itself, `ConsoleSnapshot.SavePng(app.Root, width, height, path)` renders the same
control tree to a PNG — that's how every screenshot in `review/*.png` was produced, each on its own
freshly-constructed, freshly-seeded app instance so the caption always matches what's on screen.

## What I'd tell the next person starting out

Get the threading model straight before you write anything else — one UI thread, marshal in with
`Invoke`/`Post`/`InvokeAsync`, and every property setter you write on a custom control should call
`Invalidate()` (or `SetAtomicProperty`) so your own controls repaint the same way the built-in ones do. Split your
domain logic into its own project from day one — you will want to unit-test a filter/query function without
booting a UI, and you'll thank yourself later. Don't be afraid to write a small `IRenderable` or
`CompositeControl` the moment a built-in control almost-but-not-quite matches your app's look — `ArticleRow` and
`ReaderPane` were maybe an hour each, and they're what make this look like eilmeldung instead of a generic list.
And take the headless snapshot tests seriously as *behavior* tests, not smoke tests — assert on rendered text,
not just model state; it's the only way to prove a keybinding or a layout actually does what you think it does
without a human staring at a terminal.

It's a real app now — folders, feeds, tags, saved queries, read/unread/marked state, zen mode, search, all driven
from real RSS feeds via `CodeHollow.FeedReader` — and it got there through the same handful of concepts over and
over: a tree of controls, one thread, `Invoke`/`Post` at the boundary, and a snapshot test proving each piece
actually renders what I claimed it would.
