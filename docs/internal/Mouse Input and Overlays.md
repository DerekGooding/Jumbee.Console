# Mouse Input and Overlays

This document describes the interactive-control core added on top of the rendering/layout base: per-control
**mouse events** (hover, click, double-click), **wheel scrolling**, a floating **overlay/popup layer**, and the
**modal input routing** that sits on it. It finishes with the `Select` widget, which composes all of these, and a
note on its click-outside behavior.

Everything here builds on the cell-composition and focus model described in *ConsoleGUI Control Rendering* and
*Multithreading*: all state mutation and input dispatch happen on the single UI thread, and controls are composited
by pulling cells from a `DrawingContext` tree.

## 1. How mouse events reach a control

Mouse dispatch already exists in ConsoleGUI's `ConsoleManager`; Jumbee only has to feed it and react to it.

A control tags the cells it owns with itself as the mouse listener. `Control`'s indexer
([Control.cs](../../src/Jumbee.Console/Control.cs)) does this whenever the control is focusable or has opted in:

```csharp
var cell = consoleBuffer[position];
// Tag the cell so mouse events inside it route here (click-to-focus, hover, click).
return Focusable || WantsMouse ? cell.WithMouseListener(this, position) : cell;
```

The tag rides the composited cell up the tree. Each frame `UI.OnInput` ([UI.cs](../../src/Jumbee.Console/UI.cs))
turns a decoded `MouseInputEvent` into updates on `ConsoleManager`:

```csharp
case MouseInputEvent m:
    ConsoleManager.MousePosition = new Position(m.X, m.Y);
    switch (m.Kind)
    {
        case TerminalMouseKind.Down: ConsoleManager.MouseDown = true; break;
        case TerminalMouseKind.Up:   ConsoleManager.MouseDown = false; break;
        case TerminalMouseKind.Wheel:
            ConsoleManager.MouseWheel(m.Button == TerminalMouseButton.WheelUp ? -WheelLines : WheelLines);
            break;
    }
    break;
```

`ConsoleManager` hit-tests the cell under `MousePosition` and calls `OnMouseEnter`/`Leave`/`Move` on transitions,
and `OnMouseDown`/`OnMouseUp` when `MouseDown` flips. Those land on `Control`'s `IMouseListener` implementation.

Mouse events only flow when the VT input source reports them: `VtInputSource`
([VtInputSource.cs](../../src/Jumbee.Console/Input/VtInputSource.cs)) enables SGR mouse mode. By default it uses
**1002** (motion reported only while a button is held); pass `anyMotion: true` to use **1003** (every move
reported), which is what makes idle **hover** work. Wheel notches are reported under either mode.

## 2. Step 1 — Control mouse hooks

`Control` ([Control.cs](../../src/Jumbee.Console/Control.cs)) turns the raw `IMouseListener` callbacks into a
friendlier surface: overridable hooks, events, and synthesized gestures. The interface methods are the dispatch
sink; each routes to a `protected virtual` hook and a public event, and the up-sink synthesizes click /
double-click:

```csharp
void IMouseListener.OnMouseUp(Position position)
{
    OnMouseRelease(position);
    MouseReleased?.Invoke(this, position);
    if (!IsMousePressed) return;          // release without a matching press here -> not a click
    IsMousePressed = false;

    var now = Environment.TickCount64;
    bool isDouble = now - _lastClickMs <= DoubleClickMs
        && _lastClickPos.X == position.X && _lastClickPos.Y == position.Y;
    _lastClickMs = isDouble ? 0 : now;
    _lastClickPos = position;

    if (isDouble) { OnDoubleClick(position); DoubleClicked?.Invoke(this, position); }
    else          { OnClick(position);       Clicked?.Invoke(this, position); }
    Invalidate();
}
```

Key points:

- **Hooks**: `OnMouseEnter/Leave/Move`, `OnMousePress/MouseRelease`, `OnClick`, `OnDoubleClick`. Override to react;
  defaults are no-ops.
- **Events**: `MouseEntered/Left/Moved`, `MousePressed/Released`, `Clicked`, `DoubleClicked`.
- **State** for rendering: `IsMouseOver`, `IsMousePressed` (e.g. `Button` tints its background from these).
- **Click** fires only on press+release on the *same* control; a leave between them cancels it. **Double-click**
  is two clicks within `DoubleClickMs` (400 ms) at the same position.
- **Click-to-focus** is preserved: `OnMouseDown` still calls `UI.SetFocus(this)`.
- A non-focusable control that still wants mouse (e.g. a clickable link) overrides `protected virtual bool
  WantsMouse => true` so its cells get tagged.

`Button` ([Controls/Button.cs](../../src/Jumbee.Console/Controls/Button.cs)) is the canonical consumer: it derives
`RenderableControl` (so the base indexer tags its cells), renders a label tinted by hover/press state, and raises
`Activated` from either `OnClick` or Enter/Space.

## 3. Step 2 — Wheel / scroll

The wheel is the one mechanism ConsoleGUI lacked. It is added as a *separate* optional interface so existing
`IMouseListener` implementers are untouched
([IMouseWheelListener.cs](../../ext/C-sharp-console-gui-framework/ConsoleGUI/Input/IMouseWheelListener.cs)):

```csharp
public interface IMouseWheelListener
{
    void OnMouseWheel(Position position, int delta);   // delta: negative up, positive down
}
```

`ConsoleManager.MouseWheel(delta)` dispatches a notch to the listener under the cursor if it implements the
interface. On the Jumbee side, `Control` implements it and, by default, scrolls its surrounding frame:

```csharp
protected virtual void OnMouseWheel(Position position, int delta) => Frame?.Scroll(delta);
```

So **any framed, overflowing, focusable control scrolls on the wheel for free** — `ControlFrame.Scroll(n)` is the
same offset the `Alt+Up`/`Alt+Down` keys drive. A control can override `OnMouseWheel` to consume the wheel itself
(e.g. move a selection instead of scrolling). `WheelLines` (in `UI`) is the lines per notch (3).

## 4. Step 3 — The overlay layer

`Overlay` ([Layouts/Overlay.cs](../../src/Jumbee.Console/Layouts/Overlay.cs)) wraps ConsoleGUI's `Overlay` control
(aliased `COverlay`) as a Jumbee `Layout`. It composites an optional floating popup over a persistent bottom layer;
where the popup has no content, the bottom shows through, so a small centered/anchored popup floats over the UI.

```csharp
public Overlay(ILayout bottom) : base(new COverlay())
{
    _bottom = bottom;
    control.BottomContent = bottom.CControl;
}

public void Show(Control popup)              => Show(popup, CenterIn(popup), modal: false);   // CBox center
public void Show(Control popup, int x, int y) => Show(popup, AnchorAt(popup, x, y), modal: false); // CMargin offset
```

- **Positioning** reuses ConsoleGUI primitives: `CBox` to center, `CMargin` to anchor a top-left at `(x, y)`. Both
  wrap `popup.FocusableControl`, so a framed popup's border is what gets placed.
- **Transparency / hide**: closing sets `control.TopContent = null`; a null `DrawingContext` child is treated as
  transparent (its `Contains` returns false), so the bottom shows through. No special erase is needed at the layout
  level.
- **Focus registration**: with no popup shown, `Rows`/`Columns`/the indexer **delegate to the bottom layer's 2-D
  grid** (so focus navigation and routing see the bottom's real structure); while a popup is shown they present
  **only the popup** (a single cell), so focus and input are exclusive to it until it closes.

## 5. Step 4 — Modal input routing

Modality is implemented through **focus**, not a separate focus-scope stack — which works because every `Control`
self-registers in `UI`'s focus list via its `UI.Paint` subscription, so `UI.SetFocus` already spans the popup and
the bottom layer (single-focus).

**Keyboard** while a popup is shown: `Show` focuses the popup, and `Layout.OnInput` routes input to the focused
control — so the popup receives keys and the bottom does not. The overlay can also consume a key *before* the popup
sees it, via a tunnel hook on the layout base ([Layout.cs](../../src/Jumbee.Console/Layout.cs)):

```csharp
public void OnInput(UI.InputEventArgs inputEventArgs)
{
    if (InterceptInput(inputEventArgs)) return;          // tunnel phase
    Controls.ForEach(f => f.FocusedControl?.OnInput(inputEventArgs));
}
protected virtual bool InterceptInput(UI.InputEventArgs inputEventArgs) => false;
```

`Overlay` overrides it to close on its `CloseKey` (default `Escape`, `null` disables):

```csharp
protected override bool InterceptInput(UI.InputEventArgs inputEventArgs)
{
    if (IsShowing && CloseKey is { } key && inputEventArgs.InputEvent?.Key.Key == key)
    {
        Hide();
        return true;
    }
    return false;
}
```

**Click-outside** for a non-modal popup is handled by `CloseOnFocusLost` (default true): `Show` subscribes the
popup's `OnLostFocus`, so when a click moves focus to another control the popup closes (without stealing the new
focus). See §6 for the important caveat.

**Mouse modality** is `ShowModal`. It centers the popup over a full-area scrim (`CBackground` in `ModalScrim`
color). The scrim's filled cells carry **no mouse listener**, so `GetMouseContext` returns null over them and the
clicks are swallowed — the layer beneath is never hit. A modal popup does **not** subscribe `CloseOnFocusLost`; it
stays open until `CloseKey` or an explicit `Hide()`. `IsModal` reports the current mode.

## 6. The `Select` widget and click-outside behavior

`Select` ([Controls/Select.cs](../../src/Jumbee.Console/Controls/Select.cs)) is the first widget to use the whole
stack. Closed, it renders ` value …▼` and is focusable. Clicking it (or Enter/Space) calls `Open()`, which floats a
framed `ListBox` into its `Overlay` host, anchored just below it (or above when there's no room below) — the
position is computed from the click:

```csharp
protected override void OnClick(Position position)
{
    // Record this control's top-left on screen: the click's absolute position minus its position relative to us.
    if (ConsoleManager.MousePosition is { } m)
    {
        _controlLeft = m.X - position.X;
        _controlTop = m.Y - position.Y;
    }
    Open();   // Open()/ResolveTop turn that into the dropdown anchor, honouring PopupPosition
}
```

The dropdown is a `ListBox` enhanced into an option list (`SelectedIndex`, `SelectedItem`, and the events
`SelectionChanged`/`Committed`/`Cancelled`, plus Enter/Escape and click-to-select). Committing (click or Enter)
sets the `Select` value and closes; the host overlay's `CloseKey` closes it on Escape.

### Click-outside behavior (a known limitation)

The dropdown is **non-modal**, so dismissal rides on `CloseOnFocusLost`, which only fires when a click moves focus
to *another control*. Precisely:

- **Click another control that tags a mouse listener** (a `Button`, `ListBox`, another `Select` — anything using
  the base `Control` indexer): focus moves there, the dropdown closes, value unchanged. ✅
- **Click an empty region** (no control, hence no listener under the cursor): focus does not move, so the dropdown
  **stays open**.
- **Click a control that overrides its indexer without tagging a listener** — notably `TextLabel`: same as an empty
  region, the dropdown **stays open**.
- **Click the `Select` itself**: mouse-down steals focus (closing the dropdown), then mouse-up re-fires `OnClick` →
  `Open()`, so it **reopens** rather than toggling closed.
- **Press Escape**: always closes, via the overlay's `CloseKey`.

This is inherent to the cell model: a non-modal popup keeps the bottom visible through its *empty* (transparent)
area, but an empty cell is exactly one that carries no listener — so there is nothing there to catch a click.
Escape is the reliable dismiss. Guaranteed click-anywhere-outside-to-close would need either a modal scrim (which
dims the background, wrong for a dropdown) or a transparent click-catcher layer (a visually pass-through but
hit-testable control); both were judged not worth the added complexity for now.

## 7. Renderer note: erasing emptied cells

These overlay features surfaced a latent bug in the incremental renderer. `ConsoleManager.Update`/`UpdateLegacy`
used to write a cell only when it had content, so a cell that changed from a glyph to **empty** (e.g. a closed
popup) updated the diff buffer but was never erased on screen — it persisted until a full re-init (a resize calls
`Console.Initialize()`, which is why resizing "fixed" it). The fix writes a blank for an emptied cell:

```csharp
var character = cell.Character.Content.HasValue
    ? cell.Character
    : new Character(' ', cell.Character.Foreground, cell.Character.Background, cell.Character.Decoration);
```

This affects any shrinking/removed content, not just overlays. See *ANSI and Legacy Terminal Rendering* for the
surrounding renderer detail.
