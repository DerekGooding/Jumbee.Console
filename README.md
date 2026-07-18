# Jumbee.Console
![](https://ajb.nyc3.cdn.digitaloceanspaces.com/jc/jc_demo_3.gif)

## About
[Jumbee.Console](https://github.com/allisterb/Jumbee.Console) is a .NET library for advanced TUIs that focuses on performance and ease-of-use. Inspired by libs like [ratatui](https://ratatui.rs/) and [Textual](https://textual.textualize.io/), it tries to provide a high-performance retained-mode library that is easy-to-use with idiomatic .NET GUI and Task patterns, while flexible enough to create different types of TUI applications from news readers to animated dashboards to IDEs to agent harnesses to graphics apps.


## Features

* 100% managed AOT-compatible code.
* Retained-mode GUI framework with a modern API designed to be easy to use and extend.
* Sub-ms frame rendering times and minimal CPU consumption even with complex displays like multi-tab document editing and syntax highlighting.
* Uses modern terminal features: ANSI/VT control sequences, 24-bit colour, SGR-encoded mouse (mode 1006) with motion tracking, bracketed paste (mode 2004), focus reporting (mode 1004), and the alternate-screen buffer (mode 1049).
* Also support legacy non-ANSI terminal emulators like the classic Windows console.
* Uses Spectre.Console-compatible markup, styles, text rendering, and widgets in a retained-mode rendering pipeline.
* Supports both fixed-width layouts like `Grid` and flexible, resizable layouts like `DockPanel`, `HorizontalStack`, `VerticalStack`, resizable `SplitPanel`.
* Large set of common GUI controls: menus, buttons, trees, text inputs with autocomplete, modal dialog windows, etc...., supports easy composition of controld    
* Control frames support adornments like titles, borders, margins, and scrollbars.
* Cross-platform 100% managed code terminal-emulator.
* Muti-tab editor that supports C#, JavaScript, C++, Markdown + a dozen other languages.
* Split-pane interactive editors with preview for Markdown, AsciiDoc, Mermaid documents, Mermaid embedded in Markdown.
* Visualization and graphics: Many different types of plots and graphs like candlestick & heatmap plots & bar/run charts, world maps, sub-cell drawing canvas and shapes, 3D texture rendering, and support for animation.
* Flexible themes that support styling both colors and glyphs independently.
* Headless snapshot testing: render any control or layout to text or PNG without a real terminal.

## Getting Started
See [GETTING-STARTED.md](https://github.com/allisterb/Jumbee.Console/blob/master/GETTING-STARTED.md) and the project [documentation](https://github.com/allisterb/Jumbee.Console/tree/master/docs). 