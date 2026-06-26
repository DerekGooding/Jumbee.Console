# Display Widgets

Read-only widgets for presenting information. None of them take focus or handle input — you place them in a
layout and update them from your code. All live in the `Jumbee.Console` namespace.

| Control | Shows | Size |
|---------|-------|------|
| `Sparkline` | a series of numbers as inline block bars | one cell per value, 1 row |
| `Digits` | text in large seven-segment glyphs (clocks, counters) | 3 cells wide per char, 3 rows |
| `Log` | an append-only tail of styled/renderable entries | fills its cell |

They are appearance-themed where it makes sense (bar/text styles come from the active
[theme](../internal/Theming.md) and can be overridden per instance).

## `Sparkline`

Draws a list of values as block bars (`▁▂▃▄▅▆▇█`), one cell per value, scaled against the series maximum.

```csharp
var spark = new Sparkline(3, 5, 2, 8, 6, 7, 4) { BarStyle = Style.Cyan1 };

// Update the series later (the control re-sizes to the new count):
spark.Values = [.. latestSamples];

// Pin the top of the scale so bars don't re-normalise every update:
spark.Max = 100;
```

The default bar glyphs are the eighth-block elements `▁▂▃▄▅▆▇█`, which need a font with block-element coverage
(Windows Terminal / Cascadia Mono render them fine). A **legacy console** — `cmd.exe` with a raster or Lucida
Console font — has the full block `█` and box-drawing characters but **not** the partial blocks, so they show as
missing-glyph boxes. For those terminals switch to the ASCII ramp:

```csharp
var spark = new Sparkline(samples) { Bars = Sparkline.AsciiBars };   // ".:-=+*#@"
// or supply your own ramp, ordered shortest -> tallest:
spark.Bars = " ▄█";
```

See [Snapshot Testing](../internal/Snapshot%20Testing.md) for the per-font glyph-coverage details.

## `Digits`

Renders text in large three-row glyphs — handy for clocks and counters. Supported characters are `0-9` and
`. , : - + space`; anything else renders blank.

```csharp
var clock = new Digits(DateTime.Now.ToString("HH:mm:ss")) { DigitStyle = Style.Green1 };
// later, from any thread:
clock.Text = DateTime.Now.ToString("HH:mm:ss");
```

The control is `text.Length * 3` cells wide and 3 rows tall. Its glyphs are plain `_`/`|`, so it renders in any
font.

## `Log`

An append-only "tail" view: it always shows the **most recent** entries that fit in its height, like a live
console. Entries are Spectre renderables, so log lines can be coloured/styled (pass a markup string, or any
`IRenderable`). `Write` is safe to call from any thread (it marshals onto the UI thread for you).

```csharp
var log = new Log { MaxEntries = 500 };
log.Write("[green]OK[/]   server started");        // a markup string …
log.Write("[yellow]WARN[/] disk almost full");
log.Write(new Spectre.Console.Rule("section"));    // … or any Spectre IRenderable
```

Place it in a fixed-size layout cell (e.g. a `Grid` row) so the visible window matches the cell height; the
newest lines stay pinned to the bottom. Scroll-back through older history is not yet supported.

## Putting it together

```csharp
var clock = new Digits("00:00:00");
var spark = new Sparkline(1, 2, 3, 4, 5);
var log   = new Log();

var grid = new Grid(
    rowHeights:    [3, 1, 10],
    columnWidths:  [54],
    controls:
    [
        [clock],
        [spark],
        [log],
    ]);

var run = UI.Start(grid, width: 58, height: 16);
run.Wait();
```

See `WidgetGalleryDemo` in the TestDemo project for a live version that ticks the clock, shifts the sparkline,
and appends log lines from a background loop.
