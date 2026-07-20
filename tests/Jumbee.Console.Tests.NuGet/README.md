# Jumbee.Console.Tests.NuGet

A headless CLI **smoke test for the published `Jumbee.Console` and `Jumbee.Console.Documents` NuGet packages**.

Unlike the other projects in this repo (which reference the library via local `ProjectReference`s and the
`ext/` forks), this one references **only the packages restored from nuget.org**. Their bundled fork assemblies
(ConsoleGUI, the Spectre.Console fork, ConsolePlot, …) and dependencies (NTokenizers, and — via Documents —
AdocNet and the vendored Mermaid parsers) all arrive transitively from the feed. That makes it a true end-to-end
check that the packages *as shipped* load and run.

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
- The **Documents** package — `MarkdownExtendedViewer`, `AsciiDocViewer`, `MermaidViewer`, and the interactive
  editors — each **rendered offscreen** so its parser/renderer actually runs. These pull the heaviest transitive
  dependencies and are the most trim/AOT-sensitive, so this is the check most likely to catch an AOT regression
  (and it covers future doc formats automatically, since a trimmed-away type surfaces as a render failure here).
- A real **headless `UI` loop** — start, live-update from a worker thread, hotkeys, `UI.Invoke`/`UI.Post`, stop —
  rendered to a null console so no terminal is required (CI-safe).

Exit code is `0` when every check passes, otherwise the number of failed checks.

## Running

```sh
# Test the LATEST stable release on nuget.org (JumbeeVersion defaults to the floating '*',
# so restore always resolves the newest published version — nothing to bump on each publish):
dotnet run -c Release

# Pin a specific published version to reproduce/investigate:
dotnet run -c Release -p:JumbeeVersion=0.1.2 -- --version 0.1.2
```

> If you *just* published a release and the run still resolves the previous one, NuGet is serving a cached
> registration — clear it first: `dotnet nuget locals http-cache --clear`.

The project keeps its own `Directory.Build.props` so it does **not** inherit `tests/Directory.Build.props`
(which would pull in the local `ext/` fork project references and collide with the package's bundled copies).

## AOT smoke test (opt-in)

`publish-aot.ps1` / `publish-aot.sh` run the **same `Program.cs`** but published as a **NativeAOT** native
single-file binary (restored from nuget.org), then execute the binary and assert exit code `0`. This is a
stronger guarantee than the `dotnet run` test: it proves the *published* package is AOT- and trim-clean when
consumed from the feed — the local `ProjectReference` build can't catch a package that shipped without the
trim metadata it carries implicitly.

It is **not** part of the default run because it requires a native toolchain — on Windows, VS Build Tools with
the Desktop C++ workload / linker; on Linux, `clang` + `zlib`.

```powershell
# Windows (latest release, win-x64):
./publish-aot.ps1
./publish-aot.ps1 -Version 0.1.2          # pin a specific release
./publish-aot.ps1 -Version 0.1.2 -Rid linux-x64
```

```sh
# Linux / CI (latest release, linux-x64):
./publish-aot.sh
./publish-aot.sh 0.1.2                     # pin a specific release
./publish-aot.sh 0.1.2 linux-arm64
```
