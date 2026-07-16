# Benchmark results history

Frozen results for comparison across optimization experiments. `BenchmarkDotNet.Artifacts/` is gitignored (it
holds only the *latest* run), so record durable numbers here.

Environment: BenchmarkDotNet v0.15.8, Windows 10 (19045), Intel Core i5-10300H 2.50GHz (4 physical / 8 logical),
.NET SDK 10.0.101, .NET 10.0.1 X64 RyuJIT. Params: Width=120, Height=40. Allocated = managed bytes per op.

---

## 1. Baseline — `Segment.Text` as `string` (pre-optimization)

Corresponds to the main-repo state at commit `6d5846a` (Spectre submodule unmodified).

| Benchmark          | Mean         | Allocated          |
|--------------------|-------------:|-------------------:|
| RenderTable        |   265.6 µs   | 264.31 KB (270,653 B) |
| RenderTree         |   109.8 µs   |  80.44 KB ( 82,371 B) |
| GetTableSegments   |   189.26 µs  | 262.69 KB (268,992 B) |
| GetTreeSegments    |    46.69 µs  |  77.58 KB ( 79,440 B) |
| SplitLines         |    26.54 µs  |  28.02 KB ( 28,696 B) |
| Truncate           |     0.737 µs |            856 B      |

Key fact: for a table of short cells, ~99% of the per-frame render allocation is Spectre segment/string
production inside `GetSegments` (RenderTable 264.3 KB vs GetTableSegments 262.7 KB — the buffer write adds ~1.6 KB).

---

## 2. `Segment.Text` as `ReadOnlyMemory<char>` slice (current)

Zero-copy `Split`/`Truncate`/`StripLineEndings`/`SplitOverflow` + span-based `CellCount`/`SplitLines`; public
`string Text` preserved via `MemoryMarshal.TryGetString` (Spectre submodule edits, uncommitted at time of writing).
Test-green: 968 Spectre + 406 Jumbee.

| Benchmark          | Mean         | Allocated          | vs baseline (alloc) |
|--------------------|-------------:|-------------------:|--------------------:|
| RenderTable        |   238.0 µs   | 268.43 KB (274,872 B) | **+1.6%** ⚠️        |
| RenderTree         |   104.6 µs   |  78.80 KB ( 80,691 B) | −2.0%               |
| GetTableSegments   |   186.42 µs  | 266.80 KB (273,208 B) | **+1.6%** ⚠️        |
| GetTreeSegments    |    50.00 µs  |  78.76 KB ( 80,648 B) | +1.5%               |
| SplitLines         |    29.46 µs  |  23.04 KB ( 23,592 B) | **−17.8%** ✓        |
| Truncate           |     0.529 µs |            176 B      | **−79.4%** ✓✓       |

### Interpretation
- **Wins where text is actually sliced:** `Truncate` −79% alloc / −28% time, `SplitLines` −18% alloc. These are the
  wrap/truncate layout ops (paragraphs, logs, editor, truncated cells).
- **CPU improved on the full path:** RenderTable −10%, RenderTree −5%.
- **Small alloc regression on short-cell tables (+1.6%):** `Segment` grew ~8 B/instance (`string` 8 B →
  `ReadOnlyMemory<char>` 16 B). Table rendering is segment-*count*-bound, not slice-bound, so it pays the per-segment
  tax without collecting the slice savings. Flips to a win for longer/wrapping cells.

---

## 3. Markup parser/tokenizer optimization

Two changes in the parse path (Spectre submodule, on top of section 2):
- `StringBuffer` no longer wraps a `StringReader` — it indexes the source string directly (removes a per-instance
  allocation; `StringBuffer` is created once per tokenizer AND once per word in `SplitWords`).
- `MarkupTokenizer.ReadText` emits text tokens as a single `Substring` slice of the source, only spinning up a
  `StringBuilder` on the first `]` (the rare `]]`/`[[` escape-collapse). Text tokens are usually bracket-free.

Test-green: 968 Spectre + 406 Jumbee.

### Markup parse (`new Markup(text)`) — before vs after

| Benchmark    | Mean (before → after) | Allocated (before → after) | Alloc Δ  |
|--------------|-----------------------|----------------------------|---------:|
| ParseRich    | 6.63 → 6.40–6.65 µs   | 16.30 KB → 14.78 KB        | −9.3%    |
| ParsePlain   | 3.92 → 3.44–3.54 µs   | 10.31 KB →  9.51 KB        | −7.8%    |
| ParseNested  | 3.56 → 3.32–3.43 µs   |  8.27 KB →  7.18 KB        | −13.2%   |
| ParseEscaped | 4.96 → 4.37–4.59 µs   | 11.92 KB → 11.27 KB        | −5.5%    |

Time −3 to −12%, allocation −5 to −13%. (Time StdDev is high on this i5 laptop — treat ranges loosely; the
allocation deltas are deterministic and reliable.)

### Render/Segment benchmarks — unchanged by this pass (as expected)

RenderTable 268.43 KB, GetTableSegments 273,208 B — byte-identical to section 2. `Workloads.BuildTable` parses each
cell's markup into `Markup` renderables at **build time** (`[GlobalSetup]`), so the parse cost is not inside the
measured render loop. The markup win applies to every markup *write* (`AnsiConsole.Markup`, `new Markup`,
`Table.AddRow`, etc.) at parse time, which these render benchmarks don't exercise per iteration.

### Remaining allocation in the parse path (next levers)
Parsing one short line still allocates 7–15 KB. The bulk that remains is in `Paragraph.Append` → `SplitWords`:
a `StringBuilder` per word + a `string[]` + a `List`, then a `new Segment` per word. Now that `Segment.Text` is a
`ReadOnlyMemory<char>` slice (section 2), `SplitWords`/`Paragraph.Append` could split into word *slices* and build
slice-segments, removing the per-word string and StringBuilder churn. Also candidate: `MarkupToken` is a class
allocated per token (could be a `readonly struct`).

---

---

## 4. `Paragraph`/`SplitWords` slicing + `MarkupToken` struct + `Merge` span fix

On top of section 3. Changes (Spectre submodule + `AnsiConsoleBuffer`):
- `SplitWords` gained a `ReadOnlyMemory<char>` overload yielding word **slices**; `Paragraph.Append` builds a
  slice-`Segment` per word (no per-word string). The `string[]` overload now delegates to it (Figlet).
- `MarkupToken` `class` → `readonly struct` (no heap object per token; `Current` is `Nullable<MarkupToken>`).
- `Segment.TextSpan`/`TextMemory` made `public`; slice ctor `internal`. `AnsiConsoleBuffer._Write` and
  `SegmentLine.Length` read `TextSpan` (no slice materialization).
- `Segment.Merge` appends `TextSpan` instead of `.Text` — **required**: without it, word-slicing regressed render
  ~6% because Merge re-concatenates adjacent same-style segments and was materializing every slice.

Test-green: 968 Spectre + 406 Jumbee.

### Markup parse (`new Markup(text)`) — original baseline → now

| Benchmark    | Mean (orig → now)  | Allocated (orig → now) | Alloc Δ (total) |
|--------------|--------------------|------------------------|----------------:|
| ParseRich    | 6.63 → 4.38 µs     | 16.30 KB → 9.35 KB     | −42.6%          |
| ParsePlain   | 3.92 → 1.84 µs     | 10.31 KB → 5.35 KB     | −48.1%          |
| ParseNested  | 3.56 → 2.40 µs     |  8.27 KB → 4.74 KB     | −42.7%          |
| ParseEscaped | 4.96 → 3.01 µs     | 11.92 KB → 6.70 KB     | −43.8%          |

Markup parse allocation roughly halved end-to-end (sections 3 + 4); time −33 to −53%.

### Render/Segment — neutral (regression fixed)

RenderTable 268.43 KB and GetTableSegments 273,208 B — byte-identical to section 2/3. The word-slicing win is a
parse-time win; at render, `Merge` recombines the words, so render is unchanged (the `Merge` span fix removed the
transient +15.7 KB regression). Table rendering doesn't re-parse markup, so it neither gains nor loses here.

### Render-path trace — further optimization candidates (not yet done)
From tracing `Control` → `AnsiConsoleBuffer.Write` → `GetSegments` → `Pipeline.Process` → widget `Render` →
`Segment.Merge` → `_Write`, every control render allocates three full `List<Segment>` copies + a `RenderOptions` +
a `[renderable]` array before any widget-internal allocation:
1. `AnsiConsoleBuffer.Write` copies the already-materialized `GetSegments` list again (`new List<Segment>(...)`) —
   reuse it via `as List<Segment>`. (HIGH, Jumbee, every frame, trivial/safe.)
2. `RenderOptions.Create` allocates a record per render; depends only on caps + console W/H — cacheable, invalidate
   on resize. (MEDIUM, every frame.)
3. Inner `GetSegments` builds `result` then `Merge` builds a second list — capacity hint / merge in place. (MEDIUM.)
4. `Pipeline.Process` takes a lock + caller allocates `[renderable]` even with 0 hooks — `HasHooks` fast path. (LOW.)
5. `DataTable.cs:230` probe uses `.GetSegments(...).Sum(s => s.Text.Count(...))` — LINQ + `.Text` materializes
   slices; use `TextSpan` + a plain loop. (LOW, control-specific.)

---

---

## 5. Render-path candidates #1 + #5, and a document-scale markup benchmark

- **#1 — `AnsiConsoleBuffer.Write`** reuses the already-materialized `GetSegments` list (`produced as List<Segment>`)
  instead of `new List<Segment>(...)`. Removes one full `Segment[]` copy per control render.
- **#5 — `DataTable` measure probe** ([DataTable.cs]) counts newlines via `TextSpan.Count('\n')` in a loop instead
  of `.GetSegments(...).Sum(s => s.Text.Count(...))` (LINQ + `.Text` slice materialization).
- New **`ParseCodeDocument`** benchmark: a ~60-line syntax-highlighted C# document (document-scale markup).

Test-green: 968 Spectre + 406 Jumbee.

### Measured

| Benchmark          | Mean       | Allocated |
|--------------------|-----------:|----------:|
| RenderTable        | 269.77 µs* | 269.77 KB* |
| RenderTree         | 108.2 µs   |  80.27 KB |
| ParseCodeDocument  |  78.95 µs  | 143.03 KB |
| ParseRich          |   4.69 µs  |   9.35 KB |

\* #1's saving (one `Segment[]` copy, ~1.6 KB/frame) is below the RenderTable measurement-noise floor — the
deterministic `GetTableSegments` is 273,208 B, but the full RenderTable number floats ±~1.5 KB in the non-GetSegments
part run-to-run. #1 is kept because it's strictly less work and cuts transient Gen0 garbage per frame (relevant for
high-frequency redraws / GC pauses), even though this workload can't isolate it. #5 isn't on a benchmark (runs on
measure/resize) but removes the same `.Text`-on-slices anti-pattern.

`ParseCodeDocument` (143 KB / 79 µs for ~60 styled lines) is the new document-scale baseline for markup parsing,
already reflecting sections 3–4. Compare future parser changes against it.

### Still open (from the render-path trace, §4): candidates #3 (`Merge` double-list), #4 (`Pipeline.Process`
0-hook fast path). #2 done in §6.

---

## 6. Candidate #2 — cache `RenderOptions` per console

`RenderOptions` is immutable (`init`-only; widgets create variants via `with`) and depends only on the fixed
capabilities + current buffer size. `AnsiConsoleBuffer` now caches it and rebuilds only on resize, via a new public
Spectre overload `GetSegments(this IRenderable, IAnsiConsole, RenderOptions)` that skips `RenderOptions.Create`.
Test-green: 968 Spectre + 406 Jumbee.

### Combined effect of #1 + #2 (Write-path overhead eliminated)

| Benchmark   | Baseline (§2/3) | #1+#2   | GetSegments floor |
|-------------|----------------:|--------:|------------------:|
| RenderTable | 268.43 KB       | **266.78 KB** | 266.80 KB (GetTableSegments) |
| RenderTree  |  80.25 KB       | **78.81 KB**  |  78.76 KB (GetTreeSegments)  |

Both render benchmarks now allocate essentially exactly what `GetSegments` produces — the redundant `List<Segment>`
copy (#1) and the per-frame `RenderOptions` allocation (#2) are gone (~1.5 KB/frame each). Absolute size is small,
but it's now provably at the floor (RenderTable == GetTableSegments), and it scales with control count and redraw
frequency (each control's `Write` previously allocated both per frame).

---

## 7. Syntax-highlight baseline — the C# editor burst (Phase 0)

`SyntaxHighlightBenchmarks` measures the highlight burst the editor pays on every document switch
(MultiTabCodeEditor source pane re-highlights the whole file). A synthetic ~450-line C# document. The current
path: ColorCode tokenizes → `SpectreMarkupFormatter.Format` builds a Spectre **markup string** →
`ansiConsole.Markup(str)` **re-parses** it into segments → the buffer applies them (the markup string is parsed
twice). Profile width is inflated so Spectre never word-wraps (mirrors `TextEditor.WriteText`).

| Benchmark        | Mean     | Allocated | Notes |
|------------------|---------:|----------:|-------|
| FormatCSharp     | 5.69 ms  | 3.13 MB   | tokenize + build markup string only |
| HighlightCSharp  | 7.62 ms  | 5.38 MB   | **full editor burst**: format + re-parse + apply |

The re-parse + segment apply adds **+2.25 MB / +1.9 ms** on top of the format — the markup string is a pure
intermediate. Killing it (Phase 1: direct-segment formatter, no markup string / no `Markup.Escape` / no re-parse)
targets that delta plus the escape/`ToString`/`OrderBy` allocations inside `Format`. This is the "before" number.

### Phase 1 result — `SpectreSegmentFormatter` (direct segments)

`SpectreSegmentFormatter` (ext/RazorConsole.Core.Syntax) subclasses `CodeColorizerBase` and emits Spectre
`Segment`s straight into a reused `List<Segment>` — no markup string, no `Markup.Escape`, no Spectre re-parse.
The editor applies them via the new `AnsiConsoleBuffer.Write(IReadOnlyList<Segment>)` (same `_Write` path: wrap,
`\n`, cursor). `TextEditor` now uses it for every ColorCode language (C#, Markdown, TS, SQL, HTML, CSS, XML).

| Benchmark               | Mean (before → after) | Allocated (before → after) | Δ alloc |
|-------------------------|-----------------------|----------------------------|--------:|
| Format only             | 5.69 → 4.95 ms        | 3.13 → 2.95 MB             | −6%     |
| **Full burst**          | 7.06 → **5.07 ms**    | 5.38 → **2.95 MB**         | **−45%**|

The whole re-parse+apply cost is gone: the full-burst number (2.95 MB) now **equals** the format-only number —
the buffer apply adds ~0 allocation (was +2.25 MB). Time −28%, and Gen2 collections drop to zero. Remaining
2.95 MB is inside ColorCode tokenizing + the per-token `content.ToString()` and `scopes.OrderBy(...).ToArray()`
(Phase 2 levers). Equivalence is locked by `SyntaxFormatterEquivalenceTests` (C#/HTML/SQL render a byte-for-byte
identical buffer via both formatters); 501 Jumbee tests + 14 examples green.

### Phase 2 result — zero-copy slices + skip-sort + single-pass ExpandTabs

Three cleanups in `SpectreSegmentFormatter`, all keeping the byte-for-byte equivalence:
- **Per-token zero-copy slice.** `Write` now carries the chunk as `ReadOnlyMemory<char>`; each token is a slice of
  it (`new Segment(ReadOnlyMemory<char>, ...)`, the internal zero-copy ctor) instead of `content.ToString()` — no
  per-token string. (Required un-signing the Spectre fork so the unsigned `RazorConsole.Core.Syntax` can be an
  `[InternalsVisibleTo]` friend; the `Jumbee.Console` friend entry was dropped — it uses only public API and the
  extra internals collided with `ConsoleGUI` types.)
- **Skip the OrderBy.** `scopes.OrderBy(s => s.Index).ToArray()` (a LINQ enumerator + array per scope node) runs
  only when an alloc-free O(n) `IsSortedByIndex` scan says the siblings aren't already ordered (the common case is).
- **Single-pass `ExpandTabs`.** Was 5 allocations (`new string(' ', w)` + `ToString()` + 3 chained `.Replace()`);
  now one `string.Create` pass that only expands `\t` (newline normalization was redundant — the buffer handles
  `\r`/`\n`), and **zero** allocation when a run has no tabs. Editor uses `TabWidth == 0` so this never fires there,
  but it's now covered by `SegmentFormatter_ExpandsTabs_SameAsMarkupFormatter`.

| Benchmark      | §7 Phase 1 | Phase 2   | Δ (P2) | vs original |
|----------------|-----------:|----------:|-------:|------------:|
| Format only    | 2.95 MB    | 2.73 MB   | −7%    | 3.13 → 2.73 |
| **Full burst** | 2.95 MB    | **2.73 MB** | −7%  | **5.38 → 2.73 MB (−49%)** |

Gen0 also drops (734 → 680 /1k ops). The residual 2.73 MB is dominated by ColorCode's own tokenization — which
was a NuGet package, so §8 vendors and optimizes it.

## 8. Vendored ColorCode.Core + tokenizer optimizations

ColorCode.Core 2.0.15 (the C# tokenizer, previously a NuGet `PackageReference`) is now vendored at
`ext/ColorCode.Core` (net10) so it can be modified. An **absolute** golden test (`CSharpHighlightGoldenTests`,
SHA-256 of the full highlighted buffer captured from the NuGet build) guards every change — the markup-vs-segment
equivalence test can't, since both run the same ColorCode. Four changes:
- **`RegexOptions.Compiled`** on the per-language `Regex` (`LanguageCompiler`). It's cached (built once, reused on
  every highlight), so the one-time JIT cost is amortized. **This is the headline: it ~halves highlight time.**
- **Lazy `Scope.Children`** — was a `new List<Scope>()` in *every* `Scope` ctor; most scopes are leaves. Allocate
  on first `AddChild`, reads return a shared empty list.
- **Shared empty scope list** — plain (no-scope) fragments passed `new List<Scope>()`; now `Array.Empty<Scope>()`.
- **Zero-copy fragments** — the parse handler signature changed `string` → `ReadOnlyMemory<char>`
  (`ILanguageParser`/`CodeColorizerBase.Write`/both formatters); `LanguageParser` hands out `sourceCode.AsMemory(..)`
  slices instead of `Substring(..)`. `SpectreSegmentFormatter` tokens are now slices of the *original* source.

| Benchmark      | §7 P2 alloc | §8 alloc | §7 P2 time | §8 time | vs original |
|----------------|------------:|---------:|-----------:|--------:|------------:|
| Format only    | 2.73 MB     | 2.57 MB  | ~5 ms      | 2.60 ms | 3.13 → 2.57 MB |
| **Full burst** | 2.73 MB     | **2.66 MB** | 5.93 ms | **2.66 ms** | **5.38 → 2.66 MB (−51%), 7.06 → 2.66 ms (−62%)** |

Time is the big win here (Compiled regex); the alloc delta is small because the substrings were a minor fraction —
the remaining 2.66 MB is the .NET regex engine's per-match `Match`/`Group`/`Capture` objects plus inherent
`Segment` production, both inherent to a runtime-built regex + segment pipeline (`RegexOptions.NonBacktracking`
isn't usable — the language regexes use lookarounds). 503 Jumbee tests + 14 examples green; golden hash unchanged
across all four changes.

---

## 9. Opt-in partial redraw — `Control.TracksDamage` + `Damage()`

A control that changes only part of its area can now report just the changed sub-rect(s) instead of its whole rect,
so the compositor scans a fraction of the cells. Opt-in (`TracksDamage`, default off → unchanged full-rect
behaviour); the renderer's per-cell diff is still the backstop. `PartialRedrawBenchmarks` drives the real
`ConsoleManager` compositor headlessly (no-op console + no-op ANSI sink) — each op = one full frame
(paint + composite). Two workloads at 120×40, `Track` on vs off:

| Workload            | Full redraw | Partial (opt-in) | Speedup | Note |
|---------------------|------------:|-----------------:|--------:|------|
| **Globe** (spinning)     | 578.2 µs | 517.6 µs | ~1.1×   | disc inscribed in a wide pane — only the blank margins are skipped |
| **Static scene** (moving block) | 290.1 µs | 107.4 µs | **~2.7×** | nearly the whole surface is static and skipped |

The delta is the compositor scan/diff/encode the opt-in avoids (render cost is identical both ways — both workloads
re-render their whole buffer each frame). The scan reduction is exact and deterministic
(`DamageTrackingTests` reads `ConsoleManager.LastFrameDirtyCells`): the moving sprite scans **2 of 1200** cells vs
the full 1200; the globe scans **3520 of 4800** (~27% margin skipped).

**Takeaway:** the win scales with how *localized* the animation is, not with how expensive its render is. The globe
gains little (its disc changes almost everywhere it's lit, and tiling/partial-redraw doesn't touch the raytrace
cost) — but the same mechanism makes a mostly-static scene's compositing ~2.7× cheaper, and it's a free improvement
for any control that opts in. It reuses the existing 32-rect dirty accumulator; no new control type or sub-buffers.

---

**Gap for a future experiment:** no wrap-heavy render workload yet (RenderParagraph / RenderLog with long wrapped
text) — the slice-bound case where the section 2 `Segment` change should show its largest win.

## 10. The dashboard's outage map — `Canvas.DamageTracking` + `ClearLabels()`

The MonitorDashboard's live map looked like the case section 9 said would win big: a 120×20 canvas that is almost
entirely static (a braille world coastline) with a handful of outage markers and rich markup city labels moving over
it. It refreshed by calling `Clear()`, re-adding the coastline, and re-adding the markers — reporting the whole panel
dirty *and* throwing away the cached layer raster. `MapPanelBenchmarks`, one op = one refresh frame
(rebuild + paint + composite) through the real compositor:

| Configuration | Mean | Allocated | Cells scanned |
|---------------|-----:|----------:|--------------:|
| 1. rebuild + no tracking (before) | 302.7 µs | 105.4 KB | 2400 / 2400 |
| 2. rebuild + damage tracking      | 261.6 µs | 105.4 KB |  309 / 2400 |
| 3. keep coastline + tracking (now)|  **99.9 µs** | **26.9 KB** |  190 / 2400 |

The cell counts are exact (`ConsoleManager.LastFrameDirtyCells`); the split below comes from
`MapPanelDiagnostics` (`-- --diag <mode>`), which is noisier than BDN but is the only thing that separates render
from composite:

| Configuration | Render | Composite |
|---------------|-------:|----------:|
| 1. rebuild + no tracking | ~191 µs | ~83 µs |
| 2. rebuild + damage tracking | ~217 µs | **~20 µs** |
| 3. keep coastline + tracking | **~81 µs** | ~17 µs |

**`Canvas.DamageTracking`** (new, default on) does exactly what section 9 promises — the composite drops ~4× and the
scan goes from 2400 to 309 cells. But it only buys **1.16×** on the frame, because the composite was never the
expensive half here: it was ~30% of the frame, and the diff costs ~20 µs of render to find the change.

**`Canvas.ClearLabels()`** (new) is what actually fixed the map: keeping the coastline's rasterized layers instead of
rebuilding them every refresh is ~2.4× on the render, **3.0× on the frame and 3.9× on allocations**. The outage
markers became `"•"` labels rather than `Points` shapes, since changing any shape invalidates the raster.

**Two measurement traps, both of which produced confidently wrong numbers first:**

1. *Where the diff runs.* Diffing the finished buffer in a separate pass over all 2400 cells added **~100 µs** to the
   render — more than the composite saving. Moving it into the blit loop that already resolves every cell, and
   damaging labels by their own old∪new rects, cut that to ~20 µs. A damage scheme that costs a full pass to discover
   a small change defeats itself.
2. *Synthesized equality is not free.* The per-cell snapshot was a `record struct`; its generated `Equals` routes each
   field through `EqualityComparer<T>.Default`, which for `Color?` falls back to a boxing comparer — **18 KB of
   garbage per frame** and three virtual calls per cell. A hand-written `Matches(sym, fg, bg)` using lifted `==` made
   tracking allocation-neutral (row 2 now matches row 1 exactly).

**Takeaway — and a correction to the intuition that started this.** "The whole map redraws when a label changes" was
right about the symptom and wrong about the cost: the expensive part was the canvas *re-rasterizing* the world map,
not the compositor re-scanning it. Damage tracking is necessary but not sufficient; a canvas that re-adds its static
backdrop every tick pays for it twice over, and no amount of compositor skipping touches that half. Section 9's rule
still holds — the win scales with how localized the change is — but only once the render is out of the way.

**Method note:** an earlier version of the split harness measured all three configurations in one process and
disagreed with BDN by ~2×: whichever ran last benefited from a fully-warmed JIT. It now runs one mode per process.
Where BDN and a hand-rolled loop disagree, BDN is right.
