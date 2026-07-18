# Jumbee.Console — Developer Guide

Guides for building console UIs with the Jumbee.Console control library. These docs are for **library users**;
notes on internals live under [internal/](internal/).

## Controls
- [Selection Controls](controls/Selection%20Controls.md) — checkboxes, radio buttons, switches, and the
  single-/multi-select list controls (`RadioSet`, `SelectionList`).
- [Display Widgets](controls/Display%20Widgets.md) — read-only presentation widgets: `Sparkline`, `Digits`,
  and `Log`.
- [Links](controls/Links.md) — the clickable `Link` (opens a URL / runs an action), plus wiring app-wide keys
  (`UI.RegisterHotKey`, Tab/Esc).
- [Composite Controls](controls/Composite%20Controls.md) — building a single `Control` out of several child
  controls (`CompositeControl`), e.g. `CodeEditor` (editor + line-number gutter).

## API reference

Full generated API reference for the core libraries: [api/](api/) (a namespace-grouped index of every public
type with its summary). It is produced from the XML-doc comments with docfx. Regenerate it after changing public
APIs by running, from the repo root:

```
powershell -File build-api-docs.ps1
```

That runs `docfx metadata` (per `docfx.json`) to emit one Markdown page per type, then rebuilds the
[api/README.md](api/README.md) index. Pass `-NoMetadata` to only rebuild the index from existing pages.
