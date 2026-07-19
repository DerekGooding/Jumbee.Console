# Troubleshooting

## `CS1704: An assembly with the same simple name 'Spectre.Console' has already been imported`

Jumbee.Console ships a private fork of Spectre.Console bundled inside the package. If your project *also* references the upstream `Spectre.Console` NuGet package, two assemblies claim the same identity and the compiler stops.

Remove the `Spectre.Console` package reference from your project. Jumbee.Console re-exposes the Spectre types it needs (markup, styles, `IRenderable`, `Segment`, and the widget bridges), so the one package is enough.

If you hit CS1704 while building the **repository** rather than your own app, the cause is usually the opposite: missing submodules. See the next entry.

## Build errors after cloning the repo (empty `ext/` folders)

The repository builds against vendored forks under `ext/`, wired in as git submodules. A plain clone leaves those directories empty and the build fails, often with a CS1704 or a "project not found".

Clone with submodules:

    git clone --recurse-submodules https://github.com/allisterb/Jumbee.Console

If you already cloned without them:

    git submodule update --init --recursive

## Nothing renders, or the output is garbled

The apps are full-screen TUIs and need a real terminal.

- Run them in a VT-capable terminal (Windows Terminal, or most modern Linux/macOS terminals). The legacy Windows console works, with reduced features.
- In Docker, pass `-it` so the container gets a TTY: `docker run --rm -it ...`. Without it the UI has nowhere to draw.
- Piped or redirected output (`app > log.txt`, most CI) is detected as non-interactive; the app falls back to keyboard-only, inline rendering rather than taking over the screen.

## Mouse, hover, or paste don't work

Mouse clicks, hover highlighting, bracketed paste, and focus reporting need VT input. Pass a `VtInputSource` to `UI.Start` and use a VT-capable terminal:

    UI.Start(root, input: new VtInputSource(anyMotion: true));

`anyMotion: true` turns on hover highlighting. Keyboard input works without any of this.

## Colours look wrong or washed out

Colour depth is detected from the terminal. When a terminal only reports 16 colours, the 24-bit palette is downsampled. Use a terminal that advertises truecolor (and a `TERM` such as `xterm-256color`) for the full palette.
