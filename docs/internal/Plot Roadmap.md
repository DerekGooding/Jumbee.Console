# Plot Roadmap

How the `Plot` control (Controls/Plot, backed by the forked `ext/ConsolePlot`) grows from a line/scatter
plotter into a small plot framework, and where the drawing techniques come from. Reference libraries studied:
`reference/projects/plotext-master` (Python, feature-rich) and `reference/projects/termgraph-master` (Python,
candlestick-only).

## Status — checkpoint 2026-07-06

**Shipped (all green, `--verify` 21/21, 520 unit tests):** the enabling refactors **A** (polymorphic `PlotElement`)
and **B** (per-cell `Pixel` background) are both in. Plot types: **line, scatter, stem, bar, grouped/stacked bars,
horizontal bars, histogram, candlestick, box-and-whisker, error bars, heatmap, confusion matrix**, plus **point
annotations** (fg/bg labels). **Every roadmap plot family is implemented, and change B is now exercised end to end
(annotated heatmap cells).** Jumbee `Plot` API: `AddSeries`/`AddScatter`/`AddStem`/`AddBars`/`AddGroupedBars`/
`AddStackedBars`/`AddHBars`/`AddHistogram`/`AddCandles`/`AddBox`/`AddBoxes`/`AddErrorBars`/`AddHeatmap`/
`AddConfusionMatrix`/`AddLabel`, `Configure*`, `Background`, plus `PlotBrush`, `PlotColormap` and `PlotLabelAlign` enums.
Robustness: degenerate data padded in `PlotData` (no try/catch in render); bars/boxes/heatmap cells tile exactly on
resize (shared slot ranges, `GraphGraphics.SlotColumns`/`SlotRows`/`SlotRange`).

**Heatmap (Phase 4, 2026-07-06):** `HeatSeries` + `GraphGraphics.DrawHeat` — a 2D value grid (row 0 at top) tiled
over the plot's data rectangle; each cell filled with a `█` in the colour from a `Func<double,Color>` map applied to
the value normalised into [vmin, vmax] (NaN cells blank). Jumbee `AddHeatmap(values, PlotColormap, min?, max?)` +
`PlotColormap` {Viridis, Heat, Grayscale, Cool}, each a stop-interpolated ramp (`Ramp`/`*Stops`). HeatmapExample.

**Grouped/stacked/horizontal bars (Phase 2 finished, 2026-07-06):** `MultiBarSeries` (grouped OR stacked via a flag,
holds N value-series + colours) + `GraphGraphics.DrawGroupedBars` (each x slot split into k sub-bars) / `DrawStackedBars`
(series stacked from baseline, full cells between rounded cumulative boundaries so segments abut). `HBarSeries` +
`DrawHBars` — positions on Y (`SlotRows`), values along X, left-anchored eighth-blocks (`FillRow`) for the fractional
right cell (X isn't flipped, so direct). Extracted `SlotRange` (shared by `SlotColumns`/`SlotRows`), added
`Bounds.IncludeX`. Jumbee `AddGroupedBars`/`AddStackedBars` (per-series palette via `ColorsFor`), `AddHBars`.

**Box-and-whisker + error bars (Phase 3 finished, 2026-07-06):** `BoxSeries` / `ErrorBarSeries : PlotElement` +
`GraphGraphics.DrawBoxes` / `DrawErrorBars`. Box = Q1–Q3 rectangle (`┌┐└┘│─`) with a median line (`├─┤`, own colour),
whiskers to min/max with caps (`─` + `┬`/`┴` where the whisker meets a cap or box edge), cell resolution, full RGB.
Error bar = vertical whisker (`│`) from y−errLow to y+errHigh, caps (`┬`/`┴`), centre marker (`┼`), asymmetric low/high.
Jumbee `AddBox` takes the five-number summary; `AddBoxes` computes quartiles (linear-interpolation percentiles) from
raw groups in the wrapper (pure data-prep, like `AddHistogram`); `AddErrorBars` has symmetric + asymmetric overloads.
`PlotStatisticalTests` covers glyphs + degenerate data (single value, empty group, zero error).

**Where the code lives:** new plot types + primitives in the fork (`ext/ConsolePlot/.../Plotting/` — `PlotElement`,
`Series`/`ScatterSeries`/`StemSeries`, `BarSeries`, `CandleSeries`, `PointLabel`; drawing in `GraphGraphics`).
Jumbee wrapper: `src/Jumbee.Console/Controls/Plot/{Plot,PlotImage}.cs`. Examples:
`examples/.../Examples/Controls/*PlotExample.cs`. Deep detail in the [[plot-control]] memory.

**Recommended next slices (pick per value):** every plot *family*, the annotated-heatmap overlay, and categorical
axis labels are all in — remaining work is depth/polish: (1) **datetime axis** (format tick values as dates — could
reuse the same explicit-tick path, or add an auto date formatter); (2) **subplots** (compose `Plot` controls in a
Jumbee `Grid`/`DockPanel`, no ConsolePlot change); (3) the still-**deferred log axis** (a genuinely separate job — a
log-aware `CoordinateConverter` + decade ticks; *not* unlocked by the categorical-tick work, which only supplies
explicit tick positions/labels, not a non-linear transform). *(Phases 2, 3 and 4, plus categorical axis labels, all
finished 2026-07-06.)*

## What the base gives us

ConsolePlot renders into a `ConsoleImage` — a grid of `Pixel(char, foreground)` cells — through public drawing
primitives (`ConsoleGraphics`: point/line/vertical/horizontal/string with clipping; `VirtualGraphics`: sub-cell
via a brush's `HorizontalResolution×VerticalResolution`) and a shared coordinate/axis/grid/tick/label system
(`CoordinateConverter`, `PlotData`, `PlotRenderer` — all **internal**). Sub-cell smoothing is the `IPointBrush`:
Braille 2×4, Quadrant 2×2, block 1×1. This is the *same* technique as plotext's `braille`/`hd` markers and its
`marker_factor`, so we're extending the right base.

Two base constraints drive the design:
- The coordinate/tick machinery is `internal` → new plot types must live **inside the fork** (they need
  `GraphGraphics`/`CoordinateConverter`).
- `Pixel` has **foreground only, no per-cell background** → color grids (heatmap/matrix) need a small `Pixel`
  change (see change B).
- Single-`char` cells → 2×3 "sextant"/`fhd` markers are unavailable (SMP surrogate pairs); Braille 2×4 is
  higher-res and BMP-safe anyway. See [[plot-control]] memory.

## Enabling changes

**A. Polymorphic plot elements.** *(done)* Generalized `Series` into `abstract PlotElement { Bounds GetDataBounds();
void Draw(GraphGraphics) }`. `PlotData` unions each element's bounds; `PlotRenderer` calls `element.Draw`. This
one change lets every plot type share the axes/grid/tick/coordinate system. Line = `Series : PlotElement`;
`ScatterSeries`/`StemSeries`/`BarSeries`/`CandleSeries`/`PointLabel` derive from it.

**B. Per-cell background on `Pixel`.** *(done)* Added `Pixel.BackgroundColor` + a `SetPixel` bg param + carried
through `PlotImage`'s blit into Jumbee's `Character` (pixel bg wins over the plot's overall Background). Landed for
point annotations (labels with fg/bg); also unblocks the Phase-4 heatmap/matrix/image family.

## Drawing primitives to add (in the fork)

1. **Baseline fill / bars** (plotext `fill_data`/`single_bar`) — fill a column/row to `y0` with block chars and
   **eighth-block sub-cell tops** `▁▂▃▄▅▆▇█` for smooth heights. Jumbee already uses eighth-blocks (modern
   scrollbar, Sparkline).
2. **Candlestick half-cell glyphs** (termgraph) — `│ ┃ ╽ ╿ ╻ ╹ ╷ ╵` render OHLC wick+body at half-cell vertical
   precision with up/down color. Port `CandleStickGraph._render_candle_at`.
3. **Colored cells** (plotext matrix/heatmap) — value→`Pixel.Background`, or colored `█` fallback.
4. **Points-only marker draw** for scatter (existing `IPointBrush` path, no connecting line).

## Roadmap (phased by value ÷ effort)

- **Phase 1 — line-engine wins:** Scatter (points) *(done)*, Stem (drop-to-axis + marker) *(done)*, Multiple
  series *(done)*, Log axis *(deferred — rewrites the coupled internal tick/bound-alignment pipeline for a
  log-aware converter + decade ticks; medium payoff, unlocks nothing else, so pushed after bars)*.
- **Phase 2 — bars** (primitive #1): vertical bar *(done — `BarSeries` + `GraphGraphics.DrawBars` with eighth-block
  sub-cell tops)*; histogram *(done — `Plot.AddHistogram` bins in Jumbee, reuses `DrawBars` with width 1.0, no
  ConsolePlot change)*; grouped *(done — `MultiBarSeries` grouped mode + `DrawGroupedBars`, sub-slot split)*;
  stacked *(done — `MultiBarSeries` stacked mode + `DrawStackedBars`, full-cell cumulative segments)*; horizontal
  *(done — `HBarSeries` + `DrawHBars`/`SlotRows`/left-anchored eighth-blocks `▏▎▍▌▋▊▉█`)*. **Phase 2 complete.**
- **Phase 3 — financial/statistical** (#1, #2): candlestick (OHLC) *(done — `CandleSeries` + `GraphGraphics.DrawCandles`,
  half-cell box glyphs `│┃╽╿╻╹╷╵` ported from termgraph, up/down colour)*; box-and-whisker *(done — `BoxSeries` +
  `GraphGraphics.DrawBoxes`, Q1–Q3 box + median + min/max whiskers; `AddBoxes` computes quartiles from raw groups)*;
  error bars *(done — `ErrorBarSeries` + `GraphGraphics.DrawErrorBars`, ±err whisker + caps + marker, asymmetric)*.
  **Phase 3 complete.**
- **Phase 4 — color grids** (change B): heatmap *(done — `HeatSeries` + `GraphGraphics.DrawHeat`, a 2D value grid
  tiled over the plot area, cells coloured `█` via a normalised-value→colour map; Jumbee `AddHeatmap` + `PlotColormap`
  {Viridis, Heat, Grayscale, Cool} with stop-interpolated ramps)*; confusion matrix / annotated heatmap *(done —
  `DrawHeat` takes an optional `cellText` formatter and draws the value centred in each cell with a luminance-based
  contrast colour on the cell's own colour as **background** — the first real use of change B's `DrawText` bg; Jumbee
  `AddHeatmap(..., cellText)` + `AddConfusionMatrix`)*. **Phase 4 complete.**
- **Phase 5 — decorators/utility:** point text annotations with fg/bg *(done — `PointLabel` + change B)*; categorical
  axis labels *(done — explicit `TickSettings.CustomXTicks`/`CustomYTicks` used verbatim by `PlotData` with bounds left
  unadjusted; Jumbee `SetXTicks`/`SetYTicks` + `AddConfusionMatrix` class labels)*; **live data + subplots** *(done —
  `PlotSeries` handles from `AddLiveSeries`/`AddLiveBars` with `SetData`/`SetValues`/`Push`(rolling)/`Clear`, updating
  in place off the `_config` replay and thread-safe via `UI.Invoke`; subplots are just layout composition — see the
  `LiveDashboardExample`, a 3×3 grid of framed live panels driven off the `UI.Paint` tick)*; **next:** shape/indicator
  annotations, datetime axis, log axis (deferred).

## Jumbee integration

One `Plot` control, plotext-style fluent surface — `AddSeries`/`AddScatter`/`AddStem`/`AddBars`/`AddCandles`/
`AddHeatmap` — all sharing one coordinate system and flowing through the existing replay-on-resize `_config`
list, so every type survives resizing for free (see [[plot-control]]). Each type gets one `PlotExample` in the
browser. Jumbee's existing Spectre-based `BarChart`/`Sparkline` are separate and stay.
