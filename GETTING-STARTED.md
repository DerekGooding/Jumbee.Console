# Getting Started with Jumbee.Console

Jumbee.Console is a .NET library for building TUIs that are fast and take advantage of modern terminal capabilities. It uses a retained-mode control/layout model: place controls in layouts, let the library redraw only what changed. If
you've built a desktop UI with WinForms or WPF, the model will feel familiar: a tree of controls, a single UI thread, and property changes that trigger a repaint.

> Status: pre-release (v0.1.x). APIs may still change.

## Contents
- [Requirements](#requirements)
- [Add the library to a project](#add-the-library-to-a-project)
- [Your first app](#your-first-app)
- [The essential concepts](#the-essential-concepts)
- [Updating the UI from a background thread](#updating-the-ui-from-a-background-thread)
- [Testing without a terminal](#testing-without-a-terminal)
- [Where to go next](#where-to-go-next)

## Requirements

- .NET 10 
- (Preferred) A terminal emulator that supports ANSI escape sequences and, ideally, UTF-8 and 24-bit colour — Windows Terminal, the VS Code integrated terminal, iTerm2, most modern Linux terminals. Mouse support and hover need a VT-capable terminal.

Non-ANSI terminals like the Windows legacy terminal are also supported but with degraded performance and features.


## Running examples 
Easiest way to try out the examples is to pull the [Docker image](https://hub.docker.com/repository/docker/allisterb/jumbee-console/general):

`docker run --rm -it allisterb/jumbee-console:latest` Pull the latest image and run the examples browser.

`docker run --rm -it allisterb/jumbee-console:latest agent-harness` Pull the latest image and run the agent harness example.

## Add the library to a project

`dotnet add package Jumbee.Console`

Everything you need lives in the single `Jumbee.Console` namespace (`UI`, `Grid`, `Button`, `TextLabel`, `Color`,
`VtInputSource`, …).

## Your first app

A counter: a label and a button that increments it. This is a complete `Program.cs`.

```csharp
using Jumbee.Console;

var count = 0;

var label  = new TextLabel(TextLabelOrientation.Horizontal, "Count: 0", Color.Cyan1);
var button = new Button("Increment");

button.Activated += (_, _) =>
{
    count++;
    label.Text = $"Count: {count}";   // a property change repaints on the next frame
};

// Arrange the two controls in a grid: one column, two rows (1 tall for the label, 3 for the framed button).
var root = new Grid(
    rowHeights:   [1, 3],
    columnWidths: [30],
    controls:
    [
        [label],
        [button.WithRoundedBorder(Color.Grey50)],   // wrap the button in a rounded border
    ]);

// Esc quits (Ctrl+Q already does by default); focus the button so Enter/Space activates it.
UI.RegisterHotKey(UI.HotKeys.Escape, UI.Stop);
UI.SetFocus(button);

// Start the UI. Mouse/hover need a VtInputSource; keyboard works without one.
var t = UI.Start(root, width: 34, height: 6, input: new VtInputSource(anyMotion: true)).
// Wait till the UI tops.
t.Wait();
```

Run it in a real terminal (`dotnet run`). Click **Increment** or focus it and press **Enter/Space**; press
**Esc** or **Ctrl+Q** to quit.

What's happening:
- `UI.Start(rootLayout, …)` takes over the terminal, spins up the UI thread, and renders frames until `UI.Stop()`
  is called. It returns a `Task`; `.Wait()` blocks your `Main` until the UI exits.
- Setting `label.Text` from the event handler schedules a repaint — you never call "draw" yourself.

## The essential concepts

### 1. The single UI thread (the most important thing)

All UI state lives on **one dedicated UI thread**, driven by a `Dispatcher` on a frame loop. There is no UI lock;
instead, work is *marshalled* onto that thread — exactly like WPF/WinForms.

- **Scalar property setters marshal for you.** Setting `label.Text`, `gauge.Value`, `globe.RotationAngle`, etc.
  from any thread is safe — the setter posts the change to the UI thread and requests a repaint.
- **Multi-step changes and collections must be marshalled explicitly.** For anything that isn't a single scalar
  assignment (mutating a list, changing several fields together, reading collection state), wrap it:
  - `UI.Invoke(() => { … })` — run now on the UI thread (inline if you're already on it, else posted and awaited).
  - `UI.Post(() => { … })` — fire-and-forget onto the UI thread.
  - `UI.InvokeAsync(() => { … })` — awaitable.

You rarely call `Invalidate()` yourself; property setters do it. If you write a control that needs a redraw after
some internal change, call `Invalidate()`.

### 2. Controls

Every widget derives from `Control`. The library ships a large catalogue, including:

- **Text & input:** `TextLabel`, `TextPanel`, `TextInput`, `TextEditor`, `CodeEditor`, `ChatPrompt`.
- **Buttons & toggles:** `Button`, `Checkbox`, `RadioButton`, `RadioSet`, `Switch`, `ToggleButton`,
  `SelectionList`, `Select`.
- **Lists & trees:** `ListBox`, `Tree`, `Menu`, `MenuBar`, `DataTable`.
- **Readouts & charts:** `Digits`, `Sparkline`, `Gauge`, `BarChart`, `RunChart`, `Plot`, `Badge`, `Log`.
- **Rich content:** `MarkdownViewer`, `Canvas`, `Globe`, `TerminalEmulator`, and the Spectre bridges
  (`SpectreControl<T>`, `SpectreLiveDisplay`, `SpectreTaskProgress`).

Controls auto-size to their content, so you usually let the layout place them and leave `Width`/`Height` alone.
See the full list in the [API reference](docs/api/) and the [control guides](docs/controls/).

### 3. Layouts

Controls are arranged by an `ILayout`. `UI.Start` takes a layout as the root. The common ones:

- `Grid(rowHeights, columnWidths, controls)` — rows × columns of controls (used above).
- `DockPanel` — pin one control to an edge, fill the rest with another.
- `VerticalStackPanel` / `HorizontalStackPanel` — stack controls in a line.
- `SplitPanel` — two panes with a draggable divider.
- `TabPanel` — tabbed pages.
- `Boundary` — pin a single child's size.
- `Overlay` — layer pop-ups, menus, and modal dialogs above the main content (`UI.Overlay` is the ambient one).

Layouts nest: a `Grid` cell can hold another layout wrapped in a control, and composite controls embed a layout
internally.

### 4. Frames (borders, titles, scrollbars)

Any control can be wrapped in a `ControlFrame` — a border, margin, title bar, and scrollbar — with fluent helpers
that return the control so they chain:

```csharp
editor.WithRoundedBorder(Color.Cyan1).WithTitle("Program.cs");
list.WithBorder(BorderStyle.Double).WithTitle("Items");
panel.WithFrame(borderStyle: BorderStyle.Rounded, title: "Details");
```

If a framed control's content is taller than the frame, the frame shows a scrollbar and scrolls it.

### 5. Input and focus

- **Keyboard** works with the default input source.
- **Mouse and hover** need a VT terminal and a `VtInputSource`; pass `anyMotion: true` for hover highlighting:
  `UI.Start(root, input: new VtInputSource(anyMotion: true))`.
- **Focus** decides where keystrokes go. Clicking a control focuses it (click-to-focus); set it in code with
  `UI.SetFocus(control)` and read `control.IsFocused`.
- **Built-in focus navigation:** `Ctrl+N` / `Ctrl+P` cycle focus within the current layout region, and
  `Ctrl+←/→/↑/↓` move between regions. `Tab` is intentionally *not* bound by default (apps often want it) — bind
  it yourself if you want tab traversal:
  ```csharp
  UI.RegisterHotKey(UI.HotKeys.Tab, UI.FocusNext);
  UI.RegisterHotKey(UI.HotKeys.ShiftTab, UI.FocusPrevious);
  ```

### 6. Global hotkeys

Register app-wide keys with `UI.RegisterHotKey(key, action)`; they run before the focused control sees the key.
`Ctrl+Q` (quit) and `F1` (help) are registered by default. Use the `UI.HotKeys` constants/helpers so the key
matches what the input decoder produces:

```csharp
UI.RegisterHotKey(UI.HotKeys.Escape, UI.Stop);
UI.RegisterHotKey(UI.HotKeys.Ctrl(ConsoleKey.S), Save);
```

### 7. Styling and theming

- **Per control:** set style properties directly. They take a `Style` (foreground + background + decoration), and
  a plain `Color` converts to one. Colours and styles expose the full named palette (`Color.Cyan1`, `Style.Bold`,
  `Style.Grey50`, …). Text content that accepts markup understands Spectre markup (`"[green]OK[/]"`).
- **App-wide:** set a theme once and every themed control follows it. `UI.StyleTheme` / `UI.GlyphTheme` hold the
  active theme; `UI.SetTheme(...)` swaps both at runtime and re-themes live controls. Explicit per-control style
  values you set survive a theme switch; everything you leave unset follows the theme.

### 8. App lifecycle

```csharp
UI.RegisterHotKey(UI.HotKeys.Escape, UI.Stop);   // wire a quit key
UI.Start(root, width: 100, height: 30).Wait();    // blocks until UI.Stop()
```

`UI.Start` renders on an alternate screen buffer by default (your app gets a clean screen and the shell's
scrollback is restored on exit); pass `useAlternateScreen: false` to render inline. `UI.Stop()` ends the loop and
restores the terminal.

### 9. Building your own composite control

When several controls always travel together (an editor + its gutter, a labelled input), package them as a
`CompositeControl`: build the children, arrange them in any layout, wire their events, and call `SetContent`. The
result *is* a `Control`, so it drops into any layout cell and can be framed. See
[Composite Controls](docs/controls/Composite%20Controls.md).

## Updating the UI from a background thread

Because scalar setters marshal automatically, a background loop can drive the UI directly. A live clock:

```csharp
using Jumbee.Console;

var clock = new Digits(DateTime.Now.ToString("HH:mm:ss")) { DigitStyle = Color.Green1 };

var root = new Grid(rowHeights: [3], columnWidths: [26], controls: [[clock]]);

_ = Task.Run(async () =>
{
    while (true)
    {
        await Task.Delay(1000);
        clock.Text = DateTime.Now.ToString("HH:mm:ss");   // safe from any thread
    }
});

UI.RegisterHotKey(UI.HotKeys.Escape, UI.Stop);
UI.Start(root, width: 30, height: 5).Wait();
```

For changes that aren't a single property assignment (updating a list, mutating a wrapped Spectre widget), wrap
them in `UI.Invoke(() => { … })`. If you're *authoring* a control that needs a periodic tick, the protected
`Control.Feed(tick, interval)` helper runs a repeating timer that posts to the UI thread and cancels on dispose.

## Testing without a terminal

`Jumbee.Console.Snapshot` renders a layout offscreen so you can assert on the output in a unit test or a CI smoke
check — no real terminal required:

```csharp
using Jumbee.Console.Snapshot;

var text = ConsoleSnapshot.ToText(root, width: 80, height: 24);
Assert.Contains("Count: 0", text);
```

`ConsoleSnapshot` can also save a PNG (`SavePng`) and render *after* feeding keystrokes (`ToTextAfter`).

## Where to go next

- [API reference](docs/api/) — every public type, grouped by namespace, with summaries.
- [Control guides](docs/controls/) — task-focused walkthroughs (selection controls, display widgets, links,
  composite controls).
- [Internals](docs/internal/) — architecture notes: the rendering pipeline, input model, multithreading, and
  theming.
- Examples — `examples/Jumbee.Console.Examples` is a browsable gallery (each example shown next to its source);
  `examples/Jumbee.Console.IdeDemo` and `examples/Jumbee.Console.AgentHarnessDemo` are larger, full apps.
- 