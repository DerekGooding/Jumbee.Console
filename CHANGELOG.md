# Changelog

Notable changes to Jumbee.Console. The file is packed as the NuGet release notes for all three packages, so keep the newest version on top.

## 0.1.2

### Added

- `UI.SendInput(target, key, routeGlobal)` — an opt-in overload that runs the global hotkey dispatch (keys registered with `UI.RegisterHotKey`) before routing to the focused control, mirroring the live input path. Backward-compatible; existing calls route straight to the control as before.
- `Jumbee.Console.Snapshot`: `ConsoleSnapshot.RenderAfter` and `ToTextAfter` now accept a `routeGlobal` flag, so a headless snapshot test can exercise an app's global keybindings, not just control-routed input. Build the simulated key the same way the hotkey was registered so it compares equal.

### Changed

- The bundled package README now includes a runnable first-app example and a note about the private Spectre.Console fork: do not also reference the upstream `Spectre.Console` NuGet package (the assembly identities collide and the build fails with `CS1704`).

## 0.1.1

- Initial public release. Retained-mode TUI library for .NET 10: the self-contained `Jumbee.Console` core plus the `Jumbee.Console.Documents` (Markdown/AsciiDoc/Mermaid viewers) and `Jumbee.Console.Snapshot` (headless snapshot testing) add-ons.
