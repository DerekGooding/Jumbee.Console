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

### Still open (from the render-path trace, §4): candidates #2 (cache `RenderOptions`), #3 (`Merge` double-list),
#4 (`Pipeline.Process` 0-hook fast path).

---

**Gap for a future experiment:** no wrap-heavy render workload yet (RenderParagraph / RenderLog with long wrapped
text) — the slice-bound case where the section 2 `Segment` change should show its largest win.
