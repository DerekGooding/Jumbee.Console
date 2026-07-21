# Eval findings — jc-curious / reviewer loop

Running backlog of Jumbee.Console API/doc gaps and bugs surfaced by the **jc-curious** eilmeldung-port eval loop and its **jc-curious-reviewer** critic (see `.claude/agents/jc-curious*.md`). Each item is a *candidate* fix; graduate accepted ones into `CHANGELOG.md`. This log is the backlog, not a commitment.

**Legend** — type: `doc-gap` · `capability-unknown` · `missing-feature` · `bug`. severity: `blocker` · `major` · `minor`. status: `open` · `fixed (ver)` · `documented (ver)` · `dismissed`.

---

## Round 1 — 2026-07-20 (foundation build: 3-region shell, off-thread fetch, custom row/tree/panel, 7 snapshot checks)

| ID | Sev | Type | Finding | Source | Status |
|----|-----|------|---------|--------|--------|
| R1-1 | major | doc-gap | No documented recipe for a **single-child `CompositeControl`** — the only worked example (`CodeEditor`) arranges ≥2 children via a `Layout`. Blocked wrapping one control (`ReaderPane`, `ArticleListPanel`) as a real composable control; fell back to thin wrapper classes. | jc-curious + reviewer | open |
| R1-2 | major | doc-gap | **`Grid(int[] rowHeights, int[] columnWidths, …)` sizing semantics undocumented** — fixed cells vs `0`=fill (as `DockPanel` uses) vs proportional/star. The API page explicitly says it doesn't cover this. Blocked the composable single-child-in-a-Grid path. | jc-curious | open |
| R1-3 | minor | bug / doc-gap | `SplitPanel.SplitPosition` **floors at 1 cell even with `MinFirst = 0`**, contradicting the doc's "setting SplitPosition to MinFirst collapses the first pane." Harmless visually but cost a debug cycle. Fix: allow a true 0, or document the floor. | jc-curious + reviewer | open |
| R1-4 | minor | missing-feature | **`Tree.TreeNode` (and `ListBox.ListBoxItem`) have no user-data / `Tag` slot** to bind a node/row to a domain object — forces a side `Dictionary<TreeNode, Feed>`. A generic payload (or `Tag`) would remove a bespoke pattern every real app needs. | jc-curious | open |
| R1-5 | minor | doc-gap | **`Jumbee.Console.Tree` vs `Spectre.Console.Tree` name collision** — near-certain once an app does what the docs recommend (build `IRenderable` rows, so `Spectre.Console` types are in scope). Not documented as a gotcha. | jc-curious | open |
| R1-6 | minor | doc-gap | **`MarkdownViewer` namespace is undiscoverable** — no `docs/api` page, and it lives in core (`Jumbee.Console`), not `Jumbee.Console.Documents` where the other viewers live. Only resolvable by re-reading the getting-started example's `using` list. | jc-curious | open |
| R1-7 | minor | capability-unknown | **No documented per-item "update one row" path** — `ListBox.Update()`'s contract is undocumented, so a read-state toggle rebuilds the whole list (`Clear()` + `AddItems()`). Won't scale to hundreds of rows. | jc-curious + reviewer | open |
| R1-8 | minor | capability-unknown | **`ConsoleSnapshot.ToTextAfter(Control, …)` renders the whole ambient parent tree**, not the control in isolation (a deeply-nested control still exercised sibling panes). Load-bearing but undocumented; jc-curious relied on it without knowing it was intended. | jc-curious | open |

**Round-1 status updates after round 2:** R1-7 **resolved** — `ListBoxItem.Content` setter *is* the documented per-item update path ("re-measures the list") and works cleanly; the confusion was only `ListBox.Update()`'s unclear contract. R1-1 **workaround found** (doc gap remains) — a single-child-ish composite was achievable by subclassing `CompositeControl` with nested `DockPanel`s (`ReaderPane`); still no *single-child* example in the docs.

## Round 2 — 2026-07-20 (tree→list query service in Core, real measured `IRenderable` row + per-item update, palette/reader fidelity; 11 snapshot checks)

| ID | Sev | Type | Finding | Source | Status |
|----|-----|------|---------|--------|--------|
| R2-1 | major | doc-gap / missing-feature | **No scoped/local hotkey**, and a delegate-level guard (`if (UI.Focused is TextInput) return;`) does NOT work: a matched global hotkey is marked handled and swallowed *before* the delegate runs, so a letter key never reaches a focused text field. Only fix is manual `UnregisterHotKey`/`RegisterHotKey` bracketing around every focus transition — easy to forget. Fix: a hotkey-scope/local-binding concept, or at minimum document the bracket pattern. | jc-curious | open |
| R2-2 | major | bug (packaging) | **`Jumbee.Console.Styles.xml` is not in the NuGet package** — CONFIRMED: `Jumbee.Console.0.1.3.nupkg` `lib/net10.0/` has `Jumbee.Console.Styles.dll` + `Jumbee.Console.xml` but no `Jumbee.Console.Styles.xml`. So `Color`/`IStyleTheme`/`IGlyphTheme`/`ITheme` have zero IntelliSense/doc surface for a consumer — blocks the whole theming/fidelity axis. Fix: enable `GenerateDocumentationFile` for Jumbee.Console.Styles and include its `.xml` in the bundle. **Highest-value easy fix.** | jc-curious (verified) | open |
| R2-3 | minor | bug / doc-gap | **`Tree.TreeNode.UpdateTree()` is documented as public** ("Requests a redraw of the owning tree…") **but the compiler reports `CS0122` inaccessible.** Doc/reality mismatch. Fix: make it public as documented, or correct the doc's accessibility. | jc-curious | open |
| R2-4 | minor | missing-feature | **`Tree` has no `SelectionChanged`-style event** — only `NodeActivated` (leaf, Enter/double-click). "Live-filter while arrow-navigating the tree" (standard RSS-reader UX) had to be built by polling after each forwarded key. Fix: a selection-changed event. | jc-curious | open |
| R2-5 | minor | doc-gap | **`Tree`'s initial-selection / first-keypress behavior is undocumented** (does the first `Down` select the root or its first child?) — had to determine empirically. | jc-curious | open |
| R2-6 | minor | capability-unknown | **`ListBox.Items` is `ICollection<ListBoxItem>` (not indexable)** — asserting "only row N changed" in a test needed `.ElementAt(n)` + reference-equality rather than an index compare. Minor testing friction. | jc-curious | open |

**Round-1/2 status updates after round 3:** R1-4 still open (no data/`Tag` slot — still needs side dictionaries) BUT `Tree.TreeNode` turned out to have rich, XML-documented per-node *styling* (`LeafGlyph`/`ExpandedGlyph`/`CollapsedGlyph`/`*GlyphColor`), so tree restyling was achievable — the gap is data-binding, not styling. R2-2 **narrowed**: tree styling came from `TreeNode`'s own properties (documented in `Jumbee.Console.xml`), so the missing `Styles.xml` was less blocking than feared for *this* — but the broader `Color`/`IStyleTheme`/`IGlyphTheme` surface is still undocumented, and R3-3 shows the same packaging class hits bundled Spectre too. R2-4 **reconfirmed** (see R3 note): `ListBox` has `SelectionChanged`, `Tree` doesn't — asymmetry.

## Round 3 — 2026-07-20 (single update flow + state-drift bug fix, row caching + measured-width columns, eilmeldung tree look + saved queries, isolated PNG captures; 14 snapshot checks)

| ID | Sev | Type | Finding | Source | Status |
|----|-----|------|---------|--------|--------|
| R3-1 | major | capability-unknown / doc-gap | **`UI.RegisterHotKey` is process-global static state with no per-instance scope.** Constructing a second app (what "fresh app per PNG/test capture" needs) re-registers the same keys and silently re-points an *earlier* instance's `routeGlobal` input at the *newer* one — no exception, the old instance just stops responding. Surfaced as two flaky, log-less test failures. Directly breaks the multi-instance headless-testing workflow the Snapshot story encourages. Fix: document that hotkeys are process-global (not scoped to the `UI.Start` root), and/or offer an instance-scoped hotkey table. **Ties to R2-1.** | jc-curious | open |
| R3-2 | major | missing-feature | **`TreeGuide` has no `None`/connector-less mode** — only `Ascii`/`Line`/`BoldLine`/`DoubleLine`. eilmeldung's tree has *zero* connector lines (hierarchy = icon + indent). Only workaround is dimming `guideStyle` to near-invisible, which is theme-fragile (a light theme makes the lines reappear). Fix: add `TreeGuide.None` (or a hide-guides flag). | jc-curious | open |
| R3-3 | minor | doc-gap (packaging) | **Bundled `Spectre.Console.dll` inside the core package ships with no XML docs** (only `Jumbee.Console.xml` is present) — so `GetCellWidth`/`Segment.CellCount`, the exact primitives needed for correct column-width layout (vs `string.Length`), are bare signatures with no guidance a Jumbee-only consumer could discover. Same packaging class as R2-2. Fix: consider shipping the bundled forks' XML docs, or surface the key primitives in Jumbee's own docs. | jc-curious | open |
| R3-4 | minor | capability-unknown | **No render-invocation hook** to assert "a control's `Render` was NOT re-called this frame" via the Snapshot API — so a per-item caching claim (`ArticleRow`) can be exercised (content still correct) but not directly *proven* headlessly. Fix: a render/frame counter hook for tests that assert render-frequency. | jc-curious | open |

## Round 3 reviewer notes (candidates to confirm — not yet jc-curious-verified library findings)

- **C-1 (investigate)** — `MarkdownViewer` body **clips instead of word-wrapping** inside a width-constrained split pane (wraps fine in zen/full-width). If real, a long article loses everything past the first line — a genuine wrapping bug worth confirming against the library, not just her layout.
- **Lesson (eval harness, not a library gap):** jc-curious's snapshot checks assert **model state** (indices, counts, dot glyphs), so two *visible* regressions (age column absent, tree icons = tofu) shipped "green." Tests must also assert **rendered text** (`ToText`) — the recurring "test observable behavior" principle. Also her `ArticleRow` cache key omits time-derived `Age` (would freeze in a long session) — her bug, not the library's.

## Round 4 — 2026-07-20 (modernize eilmeldung to 0.1.4 + fix the r3 visible defects; 18 snapshot checks, rendered-text assertions)

Modernization **validated the 0.1.4 doc/packaging work**: jc-curious found & adopted all of `TreeGuide.None`, `TreeNode.Tag`/`ListBoxItem.Tag`, `Tree.SelectionChanged`, `UI.HotKeys.Char`, and the now-shipped `Styles` XML (glyph default values visible) — **from the doc mirror alone**, no source. The CHANGELOG "named the exact bug in the exact code pattern" she'd shipped. Two NEW findings:

| ID | Sev | Type | Finding | Source | Status |
|----|-----|------|---------|--------|--------|
| R4-1 | major | bug | **`MarkdownViewer` does not word-wrap plain paragraph text — it clips mid-word** and drops the tail, despite its doc implying it reflows to the control width. Reproduced on a BARE `MarkdownViewer` at width 40 (not a composite/nesting issue). Confirms round-3 candidate C-1. | jc-curious (bare-control repro) | **fixed (0.1.4)** — root cause: the markdown write path clips at the buffer edge (`wrap=false`); the shared char-level `wrap` is owned by TextEditor's caret math so couldn't change. Added opt-in `AnsiConsoleBuffer.wrapWords` (word-boundaried + char fallback); `MarkdownViewer` enables it. `MarkdownViewerWrapTests` guards it. |
| R4-2 | major | doc-gap | **`ListBox` calls an item's `Render(options, maxWidth)` at a large probe width (observed ~1000), not the item's real on-screen column width.** A custom row that trusts `maxWidth` for right-alignment/multi-column layout paints past the real edge and gets clipped (the "vanished age column" root cause). Fix: clamp to `List.ActualWidth`. Undocumented — nothing in `ListBox`/`ListBoxItem` XML says which width governs layout vs. the probe measure. | jc-curious | open |

## 0.1.4 disposition (2026-07-20)

Graduated into 0.1.4 (see `CHANGELOG.md`):
- **Fixed (code/packaging):** R2-2 + R3-3 (bundle private assemblies' XML docs — Styles/Spectre/etc., verified in the packed nupkg) · R1-4 (`TreeNode.Tag` + `ListBoxItem.Tag`) · R2-3 (`TreeNode.UpdateTree()` → public) · R2-4 (`Tree.SelectionChanged`) · R3-2 (`TreeGuide.None`). New `TreeApiTests.cs` covers the four Tree/item adds; full suite 819/819.
- **Documented:** R2-1 + R3-1 (hotkey process-global scope on `UI.RegisterHotKey`) · R1-2 (`Grid` fixed-cell sizing) · R1-3 (`SplitPanel.MinFirst` 1-cell floor) · R1-1 (single-child composite = `SetContent(new Boundary(child))`) · R1-5 (`Tree`/`Spectre.Console.Tree` `CS0104` note) · R1-6 (`MarkdownViewer` is in core, not Documents). *(XML-doc changes reach the generated `docs/api/*.md` on the next `pack`/`build-api-docs`.)*

Still **open** (not in the graduated clusters, all minor): R2-5 (`Tree` initial-selection behavior undocumented) · R2-6 (`ListBox.Items` not indexable) · R3-4 (no render-invocation hook to prove caching headlessly) · C-1 (investigate `MarkdownViewer` clip-vs-wrap in a constrained pane). A bigger design item deferred past docs: **scoped/focus-routed hotkeys** (R2-1/R3-1) — 0.1.4 only documents the global model; instance-scoped input is a 0.1.5+ decision.

## Highest-value fix candidates (across all rounds)
1. **R2-2** — ship `Jumbee.Console.Styles.xml` in the package (packaging omission; unblocks the whole theming/`Color` doc surface). Easy + high value.
2. **R2-1 + R3-1** — hotkey scope: document the process-global model and the unregister/re-register pattern, and/or add scoped/instance hotkeys. Two independent rounds hit this; it breaks focused-text-entry AND multi-instance testing.
3. **R1-1 + R1-2** — `Grid` sizing semantics doc + a single-child `CompositeControl` example (blocked composable custom controls).
4. **R3-2** `TreeGuide.None`; **R2-4** `Tree.SelectionChanged` (mirror `ListBox`); **R2-3** `TreeNode.UpdateTree()` accessibility fix; **R1-4** `TreeNode`/`ListBoxItem` `Tag` data slot — a cluster of small `Tree`/item API gaps.
