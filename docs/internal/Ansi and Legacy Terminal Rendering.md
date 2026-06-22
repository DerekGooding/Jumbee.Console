# ANSI and Legacy Terminal Rendering

This document describes how `ConsoleManager` renders the same composited cell grid to two
different classes of terminal: modern **ANSI** terminals that interpret VT escape sequences, and
**legacy** terminals that don't (and so are driven through plain `System.Console` writes). The most
visible difference between the two paths is the cursor, which is hardware-native on ANSI and a
hand-drawn "software" cell on legacy.

## 1. The `AnsiEnabled` switch

A single static flag selects the path:

```csharp
// ConsoleManager
public static bool AnsiEnabled = true;
```

It is set once at startup from the `isAnsiTerminal` argument to `UI.Start` ([UI.cs](../../src/Jumbee.Console/UI.cs)):

```csharp
public static Task Start(ILayout layout, ..., bool isAnsiTerminal = true, ...)
{
    ConsoleManager.AnsiEnabled = isAnsiTerminal;
    ...
    else if (!isAnsiTerminal)
        // Legacy terminal: 16-colour System.Console output.
        ConsoleManager.Console = new SimplifiedConsole();
```

So enabling legacy mode does two things: it flips `AnsiEnabled` to `false`, and it swaps the
`IConsole` for a `SimplifiedConsole` (see §4). Input also stays keyboard-only in legacy mode, since
VT mouse/paste/focus sequences aren't available.

Both paths share everything *above* the renderer — the layout pass, the pull-based cell composition
(see *ConsoleGUI Control Rendering*), and the `ConsoleBuffer` diff. They diverge only in how a
changed cell is turned into bytes, and how the cursor is drawn.

## 2. The branch point: `Update`

Every frame, `ConsoleManager.Update(rect)` clips the dirty rectangle to the buffer/window and then
forks on the flag:

```csharp
private static void Update(Rect rect)
{
    Console.OnRefresh();
    rect = Rect.Intersect(rect, Rect.OfSize(BufferSize));
    rect = Rect.Intersect(rect, Rect.OfSize(WindowSize));

    if (!AnsiEnabled) { UpdateLegacy(rect); return; }

    // ...ANSI path...
}
```

Both paths walk the same `rect`, ask `ContentContext[position]` for each cell, and use
`_buffer.Update(position, cell)` as the diff: it returns `true` only when the cell differs from what
was last drawn, so unchanged cells are skipped. The cursor cell (`Character.IsCursor`) is always
*noted* before the diff-skip, so the cursor is found even on frames where its cell didn't change.

## 3. The ANSI path

The ANSI path builds one batch of escape sequences per frame with an `AnsiControlSequenceBuilder`
(`acsb`) and writes it to the real console off the UI thread (`Task.Run(acsb.WriteToSystemConsole)`).

* **Colour & decoration** are emitted as truecolor SGR via `WriteCharacterAnsiSequence`, which only
  emits a new colour/decoration when it differs from the running `currentFg`/`currentBg`/
  `currentDecoration` — minimizing escape-sequence churn.
* **Positioning** uses `acsb.MoveCursorTo(y, x)` only when the next changed cell isn't contiguous
  with the previous one (`y != lastY || x != lastX + 1`).
* **The cursor is the terminal's own hardware cursor.** When a cell flagged `IsCursor` is seen, its
  position drives `acsb.MoveCursorTo` + `SetCursorVisibility(true)`; the encoded DECSCUSR style and
  optional OSC 12 colour (see §5) are emitted *only on change*, so the terminal's native blink phase
  isn't reset every frame. When no cursor cell is present on a full scan, the cursor is hidden.

Because the hardware cursor blinks itself, the ANSI path needs no blink timer.

## 4. The legacy path: `UpdateLegacy`

Legacy terminals can't interpret the escape batch, so `UpdateLegacy` writes each changed cell
through the `IConsole` abstraction — concretely `SimplifiedConsole`, which maps each cell to
`System.Console` 16-colour output:

```csharp
// SimplifiedConsole.Write
var foreground = character.Foreground ?? Color.White;
var background = character.Background ?? Color.Black;
SafeConsole.WriteOrThrow(position.X, position.Y,
    ColorConverter.GetNearestConsoleColor(background),
    ColorConverter.GetNearestConsoleColor(foreground),
    content);
```

Note two consequences of this resolution: null colours become concrete white/black, and truecolor is
quantized to the nearest of the 16 console colours.

### The software cursor

There is no usable hardware cursor here — we can't reposition it per-cell without it visibly jumping,
and we can't drive a consistent blink. So the hardware cursor is kept hidden and the cursor is drawn
as a **cell**:

```csharp
if (cell.Character.IsCursor) continue;   // drawn as the software cursor below, not as a raw glyph
```

After the cell loop, `UpdateLegacy` renders the cursor cell itself. Its shape follows the DECSCUSR
style encoded on the cursor `Character` (§5):

```csharp
// RenderLegacyCursorCell, "on" phase
: style <= 2 ? new Character(content, bg, fg)   // block: invert fg/bg
: style <= 4 ? new Character('_', fg, bg)        // underline
             : new Character('|', fg, bg);       // bar
```

> **Colour resolution matters.** A block cursor renders by inverting the cell's fg/bg. On an empty
> cell both are `null`, and inverting `null`↔`null` yields a plain space (an invisible cursor). So
> `fg`/`bg` are resolved to white/black *before* inverting, guaranteeing a visible block.

### Blink

Blinking is done by us, off a wall-clock derived phase so the rate is independent of frame cadence:

```csharp
private const long BlinkHalfPeriodMs = 530;          // ~1Hz
private static bool LegacyBlinkOn() => (Environment.TickCount64 / BlinkHalfPeriodMs) % 2 == 0;
```

`TickLegacyCursorBlink` is called once per frame from `AdjustBufferSize` (on the UI thread), so the
cursor keeps blinking even on otherwise-idle frames without an extra timer or thread. It re-renders
the cursor cell only when the blink phase actually flips (`on != _legacyCursorShown`). DECSCUSR
styles `0/1/3/5` blink; `2/4/6` are steady (`style == 0 || style % 2 == 1`).

### "Cursor gone" detection

Only a **full** scan is authoritative about the cursor having disappeared. A partial update that
doesn't cover the cursor cell leaves the software cursor in place; a full update with no `IsCursor`
cell clears `_cursorPosition` (the cell it occupied was redrawn as a normal glyph by the loop). The
ANSI path makes the identical full-vs-partial distinction before hiding the hardware cursor.

## 5. Shared cursor encoding (`CursorEncoding`)

Both paths get the cursor's style and colour from the **same place**: bits packed into the high end
of the cursor cell's `Decoration`, above the real decoration flags
([Character.cs](../../ext/C-sharp-console-gui-framework/ConsoleGUI/Data/Character.cs)). This lets a
cursor cell carry its style/colour up through compositing to the renderer without any extra fields:

* `EncodeStyle` / `DecodeStyle` — DECSCUSR style 0–6 in 3 bits (shift 9).
* `WithColorFlag` / `HasColor` — a flag meaning "an explicit cursor colour is present" (the colour
  itself rides in the cell's `Foreground`).
* `StripCursorBits` — clears those high bits so they never leak into SGR decorations.

The ANSI renderer decodes the style into DECSCUSR and the colour into OSC 12, and strips the bits
before emitting SGR. The legacy renderer decodes the style to pick block/underline/bar and to decide
whether the cursor blinks. The cursor cell itself is produced by `AnsiConsoleBufferCursor.ShowCursor`
in [AnsiConsoleBuffer.cs](../../src/Jumbee.Console/AnsiConsoleBuffer.cs), which is agnostic to the
render path — the same `IsCursor` cell drives both.

## 6. Choosing a path

| | ANSI (`AnsiEnabled = true`) | Legacy (`AnsiEnabled = false`) |
|---|---|---|
| Output | Batched VT escape sequences, written off-thread | `IConsole.Write` per cell (`SimplifiedConsole`) |
| Colour | Truecolor SGR | Nearest of 16 `System.Console` colours |
| Cursor | Terminal hardware cursor (DECSCUSR + OSC 12) | Software cursor drawn as a cell |
| Blink | Terminal-native | Wall-clock tick (`TickLegacyCursorBlink`) |
| Input | VT input (mouse/paste/focus available) | Keyboard-only (`ConsoleInputSource`) |

Default is ANSI. Pass `isAnsiTerminal: false` to `UI.Start` for terminals that don't interpret
escape sequences.
