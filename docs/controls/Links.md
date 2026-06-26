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
sees them, regardless of focus. `Ctrl+Q` → quit is registered by default. Use the `UI.HotKeys` constants/helpers
so the key matches exactly what the input decoder produces.

```csharp
UI.RegisterHotKey(UI.HotKeys.Escape, UI.Stop);              // Esc quits
UI.RegisterHotKey(UI.HotKeys.Ctrl(ConsoleKey.S), Save);     // ^S saves
```

There is **no built-in Tab focus-traversal** — focus moves by **clicking** a control (click-to-focus). If you
want `Tab` to cycle focus, wire it yourself over the focusables you care about:

```csharp
var focusables = new[] { link1, link2 };
var i = 0;
UI.RegisterHotKey(UI.HotKeys.Tab, () => { i = (i + 1) % focusables.Length; UI.SetFocus(focusables[i]); });
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
