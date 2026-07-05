# Plot Roadmap

How the `Plot` control (Controls/Plot, backed by the forked `ext/ConsolePlot`) grows from a line/scatter
plotter into a small plot framework, and where the drawing techniques come from. Reference libraries studied:
`reference/projects/plotext-master` (Python, feature-rich) and `reference/projects/termgraph-master` (Python,
candlestick-only).

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

**A. Polymorphic plot elements.** Generalize `Series` into `abstract PlotElement { Bounds GetDataBounds();
void Draw(GraphGraphics) }`. `PlotData` unions each element's bounds; `PlotRenderer` calls `element.Draw`. This
one change lets every plot type share the axes/grid/tick/coordinate system. Line = `Series : PlotElement`;
`ScatterSeries`/`StemSeries`/`BarSeries`/`CandleSeries`/… derive from it.

**B. Per-cell background on `Pixel`.** Add `Color? Background` + a `SetPixel` overload, carried through
`PlotImage`'s blit into Jumbee's `Character` (which already has `Background`). Unlocks heatmaps/matrix/image and
filled-area backgrounds. Small and contained. Gated behind Phase 4.

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
- **Phase 3 — financial/statistical** (#1, #2): candlestick (OHLC, flagship demo), box-and-whisker, error bars.
- **Phase 4 — color grids** (change B): heatmap, matrix plot, confusion matrix.
- **Phase 5 — decorators/utility:** text/shape/indicator annotations at data coords (have `DrawString`),
  datetime axis, subplots (compose `Plot` controls in a Jumbee `Grid`/`DockPanel` — no ConsolePlot change).

## Jumbee integration

One `Plot` control, plotext-style fluent surface — `AddSeries`/`AddScatter`/`AddStem`/`AddBars`/`AddCandles`/
`AddHeatmap` — all sharing one coordinate system and flowing through the existing replay-on-resize `_config`
list, so every type survives resizing for free (see [[plot-control]]). Each type gets one `PlotExample` in the
browser. Jumbee's existing Spectre-based `BarChart`/`Sparkline` are separate and stay.
