# Plot Roadmap

How the `Plot` control (Controls/Plot, backed by the forked `ext/ConsolePlot`) grows from a line/scatter
plotter into a small plot framework, and where the drawing techniques come from. Reference libraries studied:
`reference/projects/plotext-master` (Python, feature-rich) and `reference/projects/termgraph-master` (Python,
candlestick-only).

## Status — checkpoint 2026-07-05

**Shipped (all green, `--verify` 14/14):** the enabling refactors **A** (polymorphic `PlotElement`) and **B**
(per-cell `Pixel` background) are both in. Plot types: **line, scatter, stem, bar, histogram, candlestick**, plus
**point annotations** (fg/bg labels). Jumbee `Plot` API: `AddSeries`/`AddScatter`/`AddStem`/`AddBars`/
`AddHistogram`/`AddCandles`/`AddLabel`, `Configure*`, `Background`, plus `PlotBrush` and `PlotLabelAlign` enums.
Browser examples: Plot, Scatter, Stem, Bar, Histogram, Candlestick, Annotations. Robustness: degenerate data padded
in `PlotData` (no try/catch in render); bars tile exactly on resize (slot-based half-open ranges).

**Where the code lives:** new plot types + primitives in the fork (`ext/ConsolePlot/.../Plotting/` — `PlotElement`,
`Series`/`ScatterSeries`/`StemSeries`, `BarSeries`, `CandleSeries`, `PointLabel`; drawing in `GraphGraphics`).
Jumbee wrapper: `src/Jumbee.Console/Controls/Plot/{Plot,PlotImage}.cs`. Examples:
`examples/.../Examples/Controls/*PlotExample.cs`. Deep detail in the [[plot-control]] memory.

**Recommended next slices (pick per value):** (1) **heatmap** — change B is done, so it's now unblocked (map
value→`Pixel.Background`, a `HeatSeries`); (2) **grouped/stacked/horizontal bars** (more `DrawBars` reuse);
(3) **box-and-whisker / error bars** (finish Phase 3); (4) the still-**deferred log axis** (invasive tick-machinery
rewrite — lowest value/effort).

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
  ConsolePlot change)*; **next:** grouped, stacked, horizontal.
- **Phase 3 — financial/statistical** (#1, #2): candlestick (OHLC) *(done — `CandleSeries` + `GraphGraphics.DrawCandles`,
  half-cell box glyphs `│┃╽╿╻╹╷╵` ported from termgraph, up/down colour)*; **next:** box-and-whisker, error bars.
- **Phase 4 — color grids** (change B): heatmap, matrix plot, confusion matrix.
- **Phase 5 — decorators/utility:** point text annotations with fg/bg *(done — `PointLabel` + change B)*;
  **next:** shape/indicator annotations, datetime axis, subplots (compose `Plot` controls in a Jumbee
  `Grid`/`DockPanel` — no ConsolePlot change).

## Jumbee integration

One `Plot` control, plotext-style fluent surface — `AddSeries`/`AddScatter`/`AddStem`/`AddBars`/`AddCandles`/
`AddHeatmap` — all sharing one coordinate system and flowing through the existing replay-on-resize `_config`
list, so every type survives resizing for free (see [[plot-control]]). Each type gets one `PlotExample` in the
browser. Jumbee's existing Spectre-based `BarChart`/`Sparkline` are separate and stay.
