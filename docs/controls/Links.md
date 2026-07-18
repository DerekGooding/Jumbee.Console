# Links

`Link` is an interactive (focusable, clickable) control that opens a URL or runs an action. It lives in the
`Jumbee.Console` namespace.

## `Link`

A `Link` activates on a **mouse click**, or on **Enter/Space while focused**. On activation it opens its `Url`
with the system default handler (the browser, on Windows) and raises `Activated`. Leave `Url` `null` to use it as
a pure action link and do the work in the event handler.

```csharp
var docs = new Link("Open the docs", "https://spectreconsole.net");
docs.Activated += (_, _) => status.Text = "Opening…";

// Action-only link (no URL): just handle the event.
var reset = new Link("Reset to defaults");
reset.Activated += (_, _) => ResetSettings();
```

The link is sized to its text and renders underlined in the theme's `Info` colour; when hovered or **focused** it
switches to reverse-video so the active link is clearly highlighted. Override `LinkStyle` / `HoverStyle` to
restyle it. URL opening is best-effort — a bad URL or a missing handler is swallowed rather than crashing the UI.

> Activation runs `Process.Start(url, UseShellExecute = true)`. Only set `Url` to destinations you trust; it is
> handed to the OS shell.

## Wiring app keys (Tab, Esc, …)

There is no dedicated key-hint/footer widget — a one-row hints bar is just formatted text, so use a `TextLabel`
(or any renderable) for that. What matters is wiring the actual keys, which is done with `UI.RegisterHotKey`.

App-wide keys are registered with `UI.RegisterHotKey(key, action)`; they are handled before the focused control
sees them, regardless of focus. Use the `UI.HotKeys` constants/helpers so the key matches exactly what the input
decoder produces.

```csharp
UI.RegisterHotKey(UI.HotKeys.Escape, UI.Stop);              // Esc quits
UI.RegisterHotKey(UI.HotKeys.Ctrl(ConsoleKey.S), Save);     // ^S saves
```

Some keys are registered by default: `Ctrl+Q` quits, `F1` toggles the help dialog, and a built-in **focus
navigation** tier moves focus without clicking — `Ctrl+N` / `Ctrl+P` cycle focus within the current layout
region, and `Ctrl+←/→/↑/↓` move spatially between regions. Clicking a control also focuses it (click-to-focus).

`Tab` is intentionally **not** bound by default (apps often want it for their own use). To get the conventional
`Tab` / `Shift+Tab` focus cycling, bind them to the built-in traversal:

```csharp
UI.RegisterHotKey(UI.HotKeys.Tab, UI.FocusNext);
UI.RegisterHotKey(UI.HotKeys.ShiftTab, UI.FocusPrevious);
```

## Putting it together

```csharp
var link  = new Link("Open the project page", "https://example.com");
var hints = new TextLabel(TextLabelOrientation.Horizontal, "Tab Focus   Enter Open   Esc Quit", Color.Grey);

var grid = new Grid(
    rowHeights:   [1, 8, 1],
    columnWidths: [54],
    controls:     [[link], [/* main content */], [hints]]);

UI.RegisterHotKey(UI.HotKeys.Escape, UI.Stop);   // make the "Esc Quit" hint real

// Mouse needs a VT terminal + a VtInputSource; keyboard activation works with the default input.
var run = UI.Start(grid, width: 58, height: 12, input: new VtInputSource(anyMotion: true));
UI.SetFocus(link);
run.Wait();
```

See `LinkDemo` in the TestDemo project for a runnable version.
