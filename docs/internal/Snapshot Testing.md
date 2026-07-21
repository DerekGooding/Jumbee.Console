# Snapshot Testing (and why to snapshot in multiple fonts)

`Jumbee.Console.Snapshot.ConsoleSnapshot` renders a control tree **headlessly** — no real terminal — into a
`ConsoleBuffer`, then converts that buffer to either a **text** snapshot or a **PNG image**. It is the backbone of
the control tests and of "visual verification" during widget development.

> For the consumer-facing introduction — asserting content, driving keys, and firing global hotkeys in a test —
> see **[Testing without a terminal](../../GETTING-STARTED.md#testing-without-a-terminal)** in the getting-started
> guide. This page assumes that and focuses on the part unique to contributors: **multi-font PNG verification**.

```csharp
using Jumbee.Console.Snapshot;

// Logical (glyph-only) snapshot — great for asserting content/layout in tests:
string text = ConsoleSnapshot.ToText(control, width: 24, height: 10);

// Pixel snapshot — what the cells actually look like rendered with a font:
ConsoleSnapshot.SavePng(control, 24, 10, "out.png");

// Drive input, then snapshot. Keys go to the control you pass (NOT to whatever UI.SetFocus targets), so pass
// the control that actually changes. ToTextAfter/RenderAfter also take full ConsoleKeyInfo lists:
string afterNav = ConsoleSnapshot.ToTextAfter(list, 24, 10, [ConsoleSnapshot.Key(ConsoleKey.DownArrow)]);

// Global hotkeys (registered with UI.RegisterHotKey) need routeGlobal: true. Build the key the way it was
// registered — UI.HotKeys.Char('r') for a bare letter, UI.HotKeys.Ctrl(ConsoleKey.S) for a combo:
string afterHotkey = ConsoleSnapshot.ToTextAfter(list, 24, 10, [UI.HotKeys.Char('r')], routeGlobal: true);
```

The headless composer goes through a local `DrawingContext` + `UI.PaintFrame`; it does **not** touch
`ConsoleManager`, so it is deterministic and safe to run in parallel-free tests. (Do not write tests that drive
`ConsoleManager.Draw` — those race with the global UI singletons. Assert logical state via snapshots instead.)

## The image renderer uses a real font — and that is the point

`ToText` only tells you *which code points* a control emitted. It cannot tell you whether those glyphs will
actually **render** on a user's terminal — that depends entirely on the **font**. `ToImage`/`SavePng` draw each
cell's glyph with a real installed font ([SnapshotImageOptions](../../src/Jumbee.Console.Snapshot/SnapshotImageOptions.cs)
controls which one):

```csharp
var opts = new SnapshotImageOptions
{
    FontFamily = "Consolas",                 // primary font to try
    FallbackFontFamilies = ["Consolas"],     // tried in order if the primary isn't installed
    CellWidth = 18, CellHeight = 34, FontSize = 26,
};
ConsoleSnapshot.SavePng(control, w, h, "consolas.png", opts);
```

> The font must be **installed on the machine running the snapshot**; resolution uses SixLabors `SystemFonts`.
> If neither `FontFamily` nor any `FallbackFontFamilies` is found it falls back to the first installed family,
> which may not be monospace — so for font-specific checks, set `FallbackFontFamilies` to just the one font you
> are testing, so a missing font fails loudly instead of silently substituting.

## Why snapshot in *multiple* fonts

Terminal emulators ship different default monospace fonts, and **their Unicode coverage varies a lot** — most
notably for the block-element and box-drawing glyphs our widgets lean on. A glyph the font lacks renders as a
"tofu" box (▯). So a widget that looks perfect in one font can be unreadable in another.

This is not hypothetical. The `Sparkline` widget defaults to the eighth-block ramp `▁▂▃▄▅▆▇█` (U+2581–U+2588).
Rendering a strict `1..8` ramp under common fonts (each with no fallback) gives:

| Font | Typical terminal(s) | `▁▂▃▄▅▆▇` (eighth blocks) | `▄` `█` (half/full) |
|------|---------------------|:---:|:---:|
| **Cascadia Mono** | Windows Terminal (default), VS Code | ✅ all render | ✅ |
| **DejaVu Sans Mono** | GNOME Terminal, many Linux | ✅ all render | ✅ |
| **Consolas** | cmd.exe/conhost (modern), VS Code on Windows | ❌ tofu | ✅ |
| **Courier New** | PuTTY, generic fallback | ❌ tofu | ✅ |
| **Liberation Mono** | Linux (Courier metric clone) | ❌ tofu | ✅ |
| **Lucida Console** | legacy cmd.exe default | ❌ tofu | ✅ |
| **Lucida Sans Typewriter** | older Windows apps | ❌ tofu | ❌ even `█` tofu |

Takeaways that fed back into the code:

- The pretty eighth-block ramp only renders on a **minority** of common fonts. The half-block `▄` and full `█`
  are far more portable. Pure ASCII (`.:-=+*#@`) is universal.
- This is why `Sparkline.Bars` is a **configurable ramp** with an ASCII preset (`Sparkline.AsciiBars`): callers on
  a legacy console aren't stuck with tofu. See [Display Widgets](../controls/Display%20Widgets.md#sparkline).
- Box-drawing (`─│┌┐`) is much more widely covered than block elements — which is why `ControlFrame` borders
  render even where a `Sparkline` does not.

## Recommended practice

When you build or change a widget that emits **any non-ASCII glyph** (block elements, box drawing, arrows,
Powerline, Braille, emoji), generate PNG snapshots under at least:

1. a **rich** font with full coverage — `Cascadia Mono` (matches Windows Terminal), and
2. a **limited** font common on legacy/default consoles — `Consolas` or `Lucida Console`.

Eyeball both for tofu. If the limited font tofus the glyph, either pick a more widely supported glyph, or make the
glyph set configurable (the `Sparkline.Bars` pattern) and provide an ASCII/half-block fallback.

A drop-in helper for a dev/verification test (delete or `[Fact(Skip=...)]` it before committing — these write
files and depend on locally installed fonts, so they are not normal assertions):

```csharp
static readonly string[] RepresentativeFonts =
[
    "Cascadia Mono",   // Windows Terminal — full block coverage
    "Consolas",        // cmd.exe / VS Code (Windows) — half-block only
    "Lucida Console",  // legacy cmd.exe — half-block only
    "DejaVu Sans Mono" // Linux — full block coverage
];

void SnapshotAllFonts(Jumbee.Console.Control control, int w, int h, string name)
{
    foreach (var font in RepresentativeFonts)
    {
        var opts = new SnapshotImageOptions
        {
            FontFamily = font,
            FallbackFontFamilies = [font],   // no silent substitution: see tofu for what it is
            CellWidth = 16, CellHeight = 30, FontSize = 22,
        };
        var safe = font.Replace(" ", "_");
        ConsoleSnapshot.SavePng(control, w, h, $@"artifacts\{name}_{safe}.png", opts);
    }
}
```

For pure-ASCII widgets (e.g. `Digits`, whose seven-segment glyphs are just `_` and `|`) a single font is enough —
the multi-font pass only earns its keep when the glyph coverage is in question.
