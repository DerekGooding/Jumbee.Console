# Input Routing

How a key press or mouse action reaches the right control — across plain controls, layouts, composite controls,
and nested layouts (e.g. a list inside a tab inside a grid).

There are **two independent pipelines**, and the single most important thing to understand is that they work
completely differently:

| | Keyboard / paste | Mouse (move / click / wheel) |
|---|---|---|
| Travels through | the **logical** layout tree, from the root | nothing — direct **spatial** cell hit-testing |
| Finds its target via | the **focused** control (`FocusedControl` chain) | the **cell under the cursor** and its tagged listener |
| Cares about nesting | yes (each layer must forward) | no (a composited cell carries its listener up as-is) |
| Reaches inactive/hidden controls | no (not focused, not in the routing path) | no (not rendered, so no cells on screen) |

Everything here runs on the single UI thread (the input reader posts decoded events onto it); see
[Multithreading](Multithreading.md). This document is about *routing/dispatch*; for the mouse **gesture** surface
(hover/press/click/double-click synthesis), the wheel hook, and the overlay/modal layer, see
[Mouse Input and Overlays](Mouse%20Input%20and%20Overlays.md).

## 1. The single entry point

Decoded terminal events are dispatched by `UI.OnInput` ([UI.cs](../../src/Jumbee.Console/UI.cs)):

```csharp
case KeyInputEvent k:
    var inputEvent = new InputEvent(k.ToConsoleKeyInfo());
    globalInputListener.OnInput(inputEvent);      // 1. global hotkeys, before any control
    if (!inputEvent.Handled)
        layout?.OnInput(inputEventArgs);          // 2. into the ROOT layout's routing
    break;
case MouseInputEvent m:
    ConsoleManager.MousePosition = new Position(m.X, m.Y);   // mouse: just update ConsoleManager
    switch (m.Kind)
    {
        case TerminalMouseKind.Down: ConsoleManager.MouseDown = true; break;
        case TerminalMouseKind.Up:   ConsoleManager.MouseDown = false; break;
        case TerminalMouseKind.Wheel: ConsoleManager.MouseWheel(±WheelLines); break;
    }
    break;
case PasteInputEvent p: layout?.OnPaste(p.Text); break;     // paste follows the keyboard path
```

So **keyboard and paste enter at the root layout only**; **mouse never touches the layout tree** — it just feeds
`ConsoleManager`, which dispatches by hit-testing.

## 2. The focus model

Focus is **single and global**. Every `Control` self-registers in `UI`'s control list via its `UI.Paint`
subscription in the constructor, so `UI` can address any control that has ever been painted regardless of where it
sits in the tree.

- `UI.SetFocus(target)` clears `IsFocused` on every other registered control and sets it on the target (and its
  `FocusableControl`). Called by click-to-focus, and by `Control.Focus()` — so **always focus through these**, never
  by assigning `IsFocused = true` directly, which would leave the previous control focused too (multiple focused
  controls → a key delivered to all of them).
- `UI.Focused` is the one control with `IsFocused == true` (or none).

At any instant exactly one leaf is focused — a button, a list, a text editor, a tab header, … — and that is where
keyboard input is steered.

## 3. The `FocusedControl` contract

Keyboard routing hinges on one member of `IFocusable`:

```csharp
IFocusable? FocusedControl { get; }   // "the input target inside me, right now — or null"
```

Each type answers it differently. This table is the heart of keyboard routing:

| Type | `FocusedControl` returns | Why |
|---|---|---|
| `IFocusable` (default) | `Focusable && IsFocused ? FocusableControl : null` | a leaf is its own target when focused |
| `Control` | `Focusable && IsFocused ? this : null` | **`this`, not `FocusableControl`** — see note below |
| `ControlFrame` | `_control.FocusedControl is not null ? this : null` | a frame stays the routing node (for scroll keys) but reports inner focus |
| `Layout<T>` (default) | `Focusable && IsFocused ? FocusableControl : null` | a plain container isn't itself a focus target |
| `CompositeControl` | the first focused descendant in its children, else `base` | delegates into the child that owns focus |
| `TabPanel` | a focused tab header, else the **active** content's `FocusedControl` | delegates into the bar or the visible tab |

> **`Control.FocusedControl` returns `this`, not `FocusableControl`.** `FocusableControl` is `Frame ?? this` — for a
> *framed* control it's the `ControlFrame`. Returning the frame here would make a frame forwarding input inward route
> back to itself → infinite recursion (a real StackOverflow we hit). `FocusedControl` = "the input target inside me";
> `FocusableControl` = "how a parent layout addresses me". Don't conflate them.

Note that `Layout<T>.FocusedControl` is **`virtual`**: the default behaves like the `IFocusable` default (a plain
container forwards nothing unless it is itself focused), but interactive layouts override it so input reaches their
descendants even when nested — that override is what makes `TabPanel` work (§7).

## 4. Keyboard routing

`Layout.OnInput` is the router ([Layout.cs](../../src/Jumbee.Console/Layout.cs)):

```csharp
public void OnInput(UI.InputEventArgs args)
{
    if (InterceptInput(args)) return;                 // tunnel phase (e.g. Overlay closing on Esc)
    foreach (var f in Controls)
    {
        f?.FocusedControl?.OnInput(args);             // deliver to each child's focused descendant (null-safe)
        if (args.InputEvent?.Handled == true) return; // stop once consumed — input belongs to one control
    }
}
```

- **Hotkeys first.** `globalInputListener` checks `GlobalHotKeys` (e.g. `Ctrl+Q → Stop`, and anything from
  `UI.RegisterHotKey`) and can mark the event handled *before* any control sees it — so hotkeys work regardless of
  focus.
- **Tunnel (`InterceptInput`).** A layout may consume a key before routing it down (e.g. `Overlay` closing on its
  `CloseKey`). Default returns false.
- **Bubble to the focused leaf.** `Controls` are the layout's children; for each, `FocusedControl?.OnInput(args)`
  delivers only to the one whose focused descendant is non-null. Because focus is single, exactly one child resolves
  to the focused leaf.

A leaf control's `Control.OnInput(UI.InputEventArgs)` runs `OnInput(InputEvent)` only when `HandlesInput` is true,
so display-only controls ignore keys. There is **no built-in Tab traversal** — focus moves by click (or explicit
`UI.SetFocus`); see [Mouse Input and Overlays](Mouse%20Input%20and%20Overlays.md) and the hotkeys/focus notes.

```
key ─▶ UI.OnInput ─▶ hotkeys ─▶ ROOT layout.OnInput
                                   ├─ InterceptInput?  (tunnel)
                                   └─ for each child:  child.FocusedControl?.OnInput
                                                          └─▶ (recurses through composites / nested layouts)
                                                                 └─▶ focused leaf.OnInput(InputEvent)
```

## 5. Mouse routing

Mouse dispatch is entirely separate and **spatial**. A control tags the cells it owns with itself, in its indexer
([Control.cs](../../src/Jumbee.Console/Control.cs)):

```csharp
var cell = consoleBuffer[position];
return Focusable || WantsMouse ? cell.WithMouseListener(this, position) : cell;   // position in THIS control's coords
```

That tag rides the composited cell up the tree — through layouts, frames, composites — unchanged, because each layer
returns the child's cell as-is. `ConsoleManager` then dispatches by hit-testing the cell under the cursor
([ConsoleManager.cs](../../ext/C-sharp-console-gui-framework/ConsoleGUI/ConsoleManager.cs)):

```csharp
MouseContext?.MouseListener?.OnMouseDown(MouseContext.Value.RelativePosition);   // on MouseDown flip
// MousePosition transitions -> OnMouseEnter / Leave / Move
public static void MouseWheel(int delta)
{
    if (MouseContext is { } ctx && ctx.MouseListener is IMouseWheelListener w)
        w.OnMouseWheel(ctx.RelativePosition, delta);                              // wheel, if the listener wants it
}
```

So a click/hover/wheel lands **directly** on the control whose cell is under the cursor — nesting is irrelevant,
because hit-testing happens on the final composited buffer. Key consequences:

- **Click-to-focus** is the bridge to the keyboard pipeline: `Control`'s `OnMouseDown` calls `UI.SetFocus(this)`, so
  clicking a control makes subsequent keys route to it.
- **A non-focusable control that still wants mouse** (e.g. a clickable `Link`) opts in with `WantsMouse => true` so
  its cells get tagged.
- **Only rendered cells are hit-testable.** A control in a hidden tab or an unshown popup has no cells on screen, so
  it cannot receive mouse — no special suppression needed.

(For how `OnMouseDown`/`Up` become hover/press/click/double-click, and how the wheel default scrolls the frame, see
[Mouse Input and Overlays](Mouse%20Input%20and%20Overlays.md).)

## 6. Composite controls

A `CompositeControl` ([CompositeControl.cs](../../src/Jumbee.Console/CompositeControl.cs)) is one `Control` made of
several child controls via an internal layout. It is a single `IFocusable` to its parent, so it overrides
`FocusedControl` to return the focused descendant:

```csharp
public override IFocusable? FocusedControl
{
    get
    {
        foreach (var c in _content.Controls)
            if (c?.FocusedControl is { } focused) return focused;
        return base.FocusedControl;
    }
}
```

That single override is what lets keyboard input route *through* the composite to the focused child (e.g. the
`CodeEditor`'s text editor). Mouse needs nothing extra: the composite's indexer returns each child's cell as-is, so
the child's listener (in child coordinates) reaches `ConsoleManager` directly. The composite wires inter-child
behaviour itself (e.g. the editor's `Changed`/`MouseWheeled` events) — there is no event bus.

## 7. Layouts and nesting

`Layout.Controls` enumerates the layout's cells (`this[row, column]` over `Rows × Columns`). For a plain container
those are its direct children. Two patterns let routing reach deeper:

- **Flattening.** A layout can override `Rows`/the indexer to surface *leaf* focusables rather than sub-containers,
  so the root's `Controls.ForEach` reaches them. `Overlay` does this (bottom-layer focusables + the popup); `TabPanel`
  does it for its headers + active content.
- **`FocusedControl` override.** When the layout is itself *nested* (a child of another layout), the parent only
  sees the one `IFocusable`, and routes through `parent → thisLayout.FocusedControl`. The default returns null unless
  the layout is itself focused — which is exactly why a nested interactive layout would dead-end. Overriding
  `FocusedControl` to return the focused descendant fixes it.

`TabPanel` ([TabPanel.cs](../../src/Jumbee.Console/Layouts/TabPanel.cs)) needs **both**, so it works as root *and*
nested:

```csharp
// flattened: all headers, then the active tab's content  (so the root's OnInput walks them)
public override int Rows => _tabs.Count + (_selectedIndex >= 0 ? 1 : 0);
public override int Columns => 1;
public override IFocusable this[int row, int column] =>
    row < _tabs.Count ? _tabs[row].Header : _tabs[_selectedIndex].Content;

// delegated: focused header, else into the active content  (so a parent layout routes through here)
public override IFocusable? FocusedControl
{
    get
    {
        foreach (var t in _tabs) if (t.Header.FocusedControl is { } h) return h;
        return _selectedIndex >= 0 ? _tabs[_selectedIndex].Content.FocusedControl : null;
    }
}
```

Only the **active** tab's content is in either path, so controls in inactive tabs receive no keyboard (and, being
unrendered, no mouse).

## 8. End-to-end: a list inside a tab inside a grid

Tree: `Grid (root) → TabPanel → "Files" tab → ListBox`.

**Mouse — click the list:** the `ListBox` cells carry its listener (composited up through the tab/grid unchanged);
`ConsoleManager` hit-tests and calls `ListBox.OnMouseDown` → `UI.SetFocus(listBox)`. Direct; the grid/tab nesting is
irrelevant.

**Keyboard — press ↓ now that the list is focused:**

```
↓ ─▶ UI.OnInput ─▶ (no hotkey) ─▶ Grid.OnInput
       └─ Controls = [TabPanel, …]
            └─ TabPanel.FocusedControl  ─▶ active content.FocusedControl ─▶ ListBox   (it's focused)
                 └─ ListBox.OnInput(↓)   → moves the selection
```

If instead a **tab header** is focused (e.g. right after start, or after clicking a label), `TabPanel.FocusedControl`
resolves to that header, and `↓`/`→` switch tabs rather than scrolling the list. Clicking back into the list moves
focus there again — the single global focus is the switch between "drive the tabs" and "drive the content".

## 9. Invariants & pitfalls

- **One focused leaf at a time.** Don't assume a container "has" focus; focus lives on the leaf, and containers only
  forward to it via `FocusedControl`.
- **Nested interactive layout?** It must override `FocusedControl` (and usually flatten `Controls`), or keyboard
  dead-ends at it. A plain container needs neither.
- **`FocusedControl` ≠ `FocusableControl`.** Target-inside-me vs how-my-parent-addresses-me. Returning the wrong one
  around a `ControlFrame` recurses infinitely.
- **Mouse follows pixels, not the tree.** If a control receives unexpected clicks (or none), reason about *which
  cell's listener* is on screen there, not about the layout hierarchy.
- **Hidden = unreachable, for free.** Inactive tabs / closed popups are neither rendered (no mouse) nor in the
  focus/routing path (no keyboard); no explicit gating required.

## See also
- [Mouse Input and Overlays](Mouse%20Input%20and%20Overlays.md) — mouse gesture synthesis, the wheel hook, overlays, modal routing.
- [Multithreading](Multithreading.md) — the single UI thread that all input dispatch runs on.
- [ConsoleGUI Control Rendering](ConsoleGUI%20Control%20Rendering.md) — the cell-composition model the mouse listeners ride on.
