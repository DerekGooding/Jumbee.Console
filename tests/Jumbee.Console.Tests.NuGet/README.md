# Jumbee.Console.Tests.NuGet

A headless CLI **smoke test for the published `Jumbee.Console` NuGet package**.

Unlike the other projects in this repo (which reference the library via local `ProjectReference`s and the
`ext/` forks), this one references **only the package restored from nuget.org**. Its bundled fork assemblies
(ConsoleGUI, the Spectre.Console fork, ConsolePlot, …) and its single real dependency (NTokenizers) all arrive
transitively from the feed. That makes it a true end-to-end check that the package *as shipped* loads and runs.

## What it does

Constructs and drives the major control families and subsystems, one isolated check each:

- Styles / `Color` / markup
- Layouts (`Grid`, `VerticalStackPanel`, `DockPanel`, `SplitPanel`, `Overlay`)
- Text (`TextInput`, `TextEditor`, `CodeEditor`, `MultiTabCodeEditor`)
- Collections (`ListBox`, `Tree`, `DataTable`, `TabPanel`)
- Buttons and toggles (`Button`, `Checkbox`, `Switch`, `RadioSet`, `SelectionList`, `Select`)
- Display widgets (`Log`, `Sparkline`, `Digits`, `Badge`, `Spinner`, `Link`)
- Composite / agent controls (`ChatPrompt`, `MenuBar`, `Footer`, `ContextMenu`)
- Theming (`DefaultStyleTheme` / `DefaultGlyphTheme`, live `UI.SetTheme`)
- The Spectre.Console bridge (`AnsiConsoleBuffer`, `SpectreControl`)
- A real **headless `UI` loop** — start, live-update from a worker thread, hotkeys, `UI.Invoke`/`UI.Post`, stop —
  rendered to a null console so no terminal is required (CI-safe).

Exit code is `0` when every check passes, otherwise the number of failed checks.

## Running

```sh
# Test the pinned version (JumbeeVersion in the .csproj — keep it in step with
# ProjectAssemblyVersion in src/Directory.Build.props):
dotnet run -c Release

# Test a specific published version:
dotnet run -c Release -p:JumbeeVersion=0.1.2 -- --version 0.1.2
```

The project keeps its own `Directory.Build.props` so it does **not** inherit `tests/Directory.Build.props`
(which would pull in the local `ext/` fork project references and collide with the package's bundled copies).
