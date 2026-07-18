# Theming and Live Theme Switching

This document describes how Jumbee.Console controls are themed: the two theme interfaces, the styling primitives
they hand out, how a control captures them, how a theme can be switched **at runtime**, and how per-control
overrides survive a switch. It finishes with a step-by-step recipe for making a new control theme-aware.

The cardinal rule: **a theme changes appearance only.** It never changes a control's behaviour, and the same
theme is never allowed to require a different code path in a control. The second rule is about performance: a
control reads the theme **only when it is constructed or re-themed**, never on the render path — so theming costs
nothing per frame.

## 1. Two themes, one split

Appearance is described by two independent interfaces, both in the `Jumbee.Console.Styles` project:

- **`IGlyphTheme`** ([IGlyphTheme.cs](../../src/Jumbee.Console.Styles/IGlyphTheme.cs)) — *only* the literal glyph
  strings that get rendered: the checkbox/radio/switch markers and the scrollbar glyphs (`ScrollBarGlyphs`).
- **`IStyleTheme`** ([IStyleTheme.cs](../../src/Jumbee.Console.Styles/IStyleTheme.cs)) — *everything else about
  appearance*: the semantic `Style` colour tokens (`Text`, `BorderText`, `TitleText`, `Selection`, `Primary`, …)
  **and** the non-glyph style selectors (`FrameBorder`, `TitleStyle`). Think of it as "the general appearance
  theme"; only literal glyphs live in `IGlyphTheme`.

They are kept separate so a glyph theme and a style theme can be mixed freely. Both use **default interface
methods**, so a custom theme overrides only the members it cares about and adding a new token never breaks an
existing theme:

```csharp
// A theme that only changes the accent colour and the checkbox glyph; everything else stays default.
sealed class MyStyleTheme : IStyleTheme { public Style TextAccent => Style.Magenta1; }
sealed class MyGlyphTheme : IGlyphTheme { public string CheckboxChecked => "[*]"; }
```

Because the defaults are default-interface-method implementations, you must read a theme **through the interface
type** (e.g. `UI.StyleTheme`), not through a concrete class — a concrete `DefaultStyleTheme` does not expose the
DIM members directly.

### Styling primitives

Both interfaces hand out primitive types that also live in `Jumbee.Console.Styles`:

| Type | Carries | Source |
|------|---------|--------|
| `Color` | RGB (3 bytes) | [Color.cs](../../src/Jumbee.Console.Styles/Color.cs) |
| `Style` | foreground + background + decoration (wraps a Spectre `Style`) | [Style.cs](../../src/Jumbee.Console.Styles/Style.cs) |
| `BorderStyle` | a frame border shape selector (`None`/`Rounded`/`Heavy`/…) | [BorderStyle.cs](../../src/Jumbee.Console.Styles/BorderStyle.cs) |
| `TitleStyle` | title position + border placement + Normal/Reverse colouring | [TitleStyle.cs](../../src/Jumbee.Console.Styles/TitleStyle.cs) |
| `ScrollBarGlyphs` | the 4 scrollbar glyph strings (thumb/track/arrows) | [ScrollBarGlyphs.cs](../../src/Jumbee.Console.Styles/ScrollBarGlyphs.cs) |
| `ScrollBarStyle` | the 4 scrollbar part `Style`s (colour/decoration) | [ScrollBarStyle.cs](../../src/Jumbee.Console.Styles/ScrollBarStyle.cs) |

The scrollbar is deliberately split the same way the themes are: the **glyphs** come from `IGlyphTheme.ScrollBar`
and the **colours** from `IStyleTheme.ScrollBar`; a `ControlFrame` composes them into its scrollbar cells.

`Style` is the unit controls actually compose with. It supports `|` (combine — the right operand wins for set
properties) and a `Style.Bg(color)` helper, so a control can build state-dependent styles cheaply:

```csharp
var rowStyle = highlighted ? _selectionStyle : _textStyle;
var indicator = _checked ? _accentStyle : _mutedStyle;
if (hovered) indicator |= _hoverStyle;   // overlay the hover background, keep the foreground
```

## 2. The active theme: `UI.StyleTheme` / `UI.GlyphTheme`

The two active themes are independent statics on `UI` ([UI.cs](../../src/Jumbee.Console/UI.cs)), initialized to the
built-in defaults:

```csharp
public static IStyleTheme StyleTheme { get; set; } = new DefaultStyleTheme();
public static IGlyphTheme GlyphTheme { get; set; } = new DefaultGlyphTheme();
```

**Assigning either property raises `ThemeChanged` (on the UI thread), so it is itself a live theme switch** — every
existing control re-captures (§4). Set them before constructing controls to configure the UI at startup (there are
simply no controls to notify yet); set them later to re-skin everything live. `UI.SetTheme` (§4) is just a
convenience that assigns both.

## 3. How a control captures the theme

A control captures the theme into plain fields, then renders from those fields. The capture is centralized in a
single overridable method so the constructor and a runtime re-theme share the exact same code:

```csharp
// Control.cs — the hook. Default is a no-op for controls that don't use the theme.
protected virtual void ApplyTheme() {}
```

A themed control moves its theme reads into an `ApplyTheme` override and calls it from its constructor:

```csharp
// Button.cs
public Button(string text)
{
    _text = text;
    Width = LabelWidth(text);
    Height = 1;
    ApplyTheme();                // capture from the theme
}

protected override void ApplyTheme()
{
    if (!IsThemeOverridden(nameof(Style)))
        _style = _role == ButtonRole.Secondary ? UI.StyleTheme.SecondaryButton : UI.StyleTheme.PrimaryButton;
}
```

`Style` is a single composite `ButtonStyle` token (its per-state fills — `Normal`/`Hover`/`Press`), so one theme
read captures the whole appearance. `Render` then only reads `_style` — never the theme. (`IsThemeOverridden` is
explained in §5; ignore it for now.)

### Glyph width drives layout

Glyphs are appearance, but a glyph of a different cell width changes a control's *size*. So a themed glyph must
flow into layout, not just colour. The toggle base captures glyphs through a helper that measures them and
re-sizes the control:

```csharp
// ToggleButton.cs
protected void SetGlyphs(string on, string off)
{
    _on = on;
    _off = off;
    _indicatorWidth = IGlyphTheme.CellWidth(on, off);   // measured, not assumed
    RefreshWidth();                                     // -> Width setter -> Resize -> relayout
}
```

This is why the toggle subclasses set their glyphs inside `ApplyTheme` (so a re-theme re-measures and resizes),
and why a control must never hard-code an indicator width.

## 4. Live theme switching

Assigning `UI.StyleTheme` or `UI.GlyphTheme` raises `ThemeChanged`, which is what makes every live control
re-capture. `UI.SetTheme` is just a convenience that assigns both:

```csharp
// UI.cs
public static event EventHandler? ThemeChanged;

public static IStyleTheme StyleTheme
{
    get => _styleTheme;
    set => Invoke(() => { _styleTheme = value; ThemeChanged?.Invoke(null, EventArgs.Empty); });
}
// GlyphTheme is the same shape.

public static void SetTheme(IStyleTheme styleTheme, IGlyphTheme glyphTheme)
{
    StyleTheme = styleTheme;   // each setter raises ThemeChanged
    GlyphTheme = glyphTheme;
}
```

For callers that want to customise both halves as one unit, `ITheme`
([ITheme.cs](../../src/Jumbee.Console.Styles/ITheme.cs)) bundles an `IStyleTheme Styles` and an `IGlyphTheme Glyphs`
(both default-implemented, so a theme may supply only one side), and `SetTheme(ITheme)` applies both:

```csharp
sealed class DarkTheme : ITheme
{
    public IStyleTheme Styles => new DarkStyleTheme();
    public IGlyphTheme Glyphs => new AsciiGlyphTheme();
}
UI.SetTheme(new DarkTheme());
```

Two things make this safe and cheap:

- **The event is raised on the UI thread** (the setters wrap the field write + notification in `Invoke`), so
  handlers mutate control fields without racing the renderer — even when a theme is assigned from a background thread.
- **Re-capture happens only on assignment**, a rare event — the render path still reads plain fields, so the
  "no theme reads per frame" rule is preserved. (This is why live switching does *not* contradict the
  capture-once design: capture-once was about the render path, not about switching.)

Because each setter fires, `SetTheme` notifies twice (once per theme); that just runs each control's `ApplyTheme`
twice, which is idempotent. When called from the UI thread (the usual case) both fire synchronously before the
next frame, so no intermediate mixed-theme state is ever rendered.

`Control` subscribes to the event exactly like it subscribes to `Paint`, and re-applies + repaints when it fires:

```csharp
// Control.cs — in the constructor
UI.ThemeChanged += OnThemeChanged;
// ...and in Dispose:  UI.ThemeChanged -= OnThemeChanged;

private void OnThemeChanged(object? sender, EventArgs e)
{
    ApplyTheme();   // re-capture (glyph-width changes flow through SetGlyphs -> Resize -> relayout)
    Invalidate();   // repaint
}
```

Subscriptions must be removed in `Dispose` so the static event doesn't pin dead controls — the same contract the
`Paint` event has.

## 5. Surviving a switch: `ThemeOverrides`

A control captures theme defaults, but a caller may then override a token (`new Checkbox { AccentStyle = Red }`).
A naive re-theme would clobber that override. To preserve it, each control tracks which themeable properties were
explicitly set, and `ApplyTheme` re-applies the theme **only** to the ones that weren't.

The tracker is a small shared helper ([ThemeOverrides.cs](../../src/Jumbee.Console/ThemeOverrides.cs)) — a
`Dictionary<string,bool>` keyed by property name, with `Mark` / `IsOverridden`.

Marking is wired into `SetAtomicProperty` (the helper every visual setter already uses). A themeable setter opts
in with `themeOverride: true`; the property name is captured automatically:

```csharp
// Control.cs
protected T SetAtomicProperty<T>(ref T field, T value, /* ... */,
                                 bool themeOverride = false, [CallerMemberName] string? propertyName = null)
{
    if (themeOverride && propertyName is not null) _themeOverrides.Mark(propertyName);
    // ...equality check, assign, invalidate...
}

// AccentStyle setter — one flag, no name to type:
public Style AccentStyle { get => _accentStyle; set => SetAtomicProperty(ref _accentStyle, value, themeOverride: true); }
```

`[CallerMemberName]` resolves to the **logical member** inside a property accessor, i.e. `"AccentStyle"`, *not*
`"set_AccentStyle"` — so the marked name matches the `nameof(AccentStyle)` that `ApplyTheme` checks, with no way
to mismatch them. `ApplyTheme` then guards each field:

```csharp
if (!IsThemeOverridden(nameof(AccentStyle))) _accentStyle = UI.StyleTheme.TextAccent;
```

The key invariant: **`ApplyTheme` assigns fields directly (never through the setters)**, so re-applying the theme
never marks anything. Only genuine caller assignments mark. The net effect: an explicit `AccentStyle = Red`
survives a theme switch, while the control's un-set siblings (label, muted, hover) follow the new theme.

Non-themeable setters (`Text`, `IsChecked`, `SelectedIndex`, …) simply omit `themeOverride`, so they're never
tracked. Glyphs on the toggle controls have no override API, so they always follow the glyph theme.

## 6. `ControlFrame`

A `ControlFrame` ([ControlFrame.cs](../../src/Jumbee.Console/ControlFrame.cs)) is not a `Control`, but it is themed
the same way: its border shape/colour come from `IStyleTheme.FrameBorder`/`BorderText`, its title from
`TitleStyle`/`TitleText`, and its scrollbar from `IGlyphTheme.ScrollBar` + `IStyleTheme.ScrollBar` (composed into
`Character` cells by `RecomposeScrollBar`). It has its own `_themeOverrides`, its setters mark explicitly
(`_themeOverrides.Mark(nameof(BorderStyle))`), and its `ApplyTheme` re-applies only the un-overridden parts.

The frame's `ThemeChanged` subscription is managed by the owning control's `Frame` setter, so the lifecycle
follows attachment:

```csharp
// Control.cs
public ControlFrame? Frame
{
    get => field;
    set
    {
        if (ReferenceEquals(field, value)) return;
        if (field is not null) UI.ThemeChanged -= field.OnThemeChanged;   // detach the old frame
        field = value;
        if (value is not null) UI.ThemeChanged += value.OnThemeChanged;   // attach the new one
    }
}
```

(`Control.Dispose` also detaches the current frame.) One consequence worth knowing: the `WithFrame`/`WithBorder`
extensions assign **only the arguments actually supplied** — they no longer self-assign `frame.X = arg ?? frame.X`,
because that would fire the themeable setter and wrongly mark an unspecified property as overridden. So
`WithRoundedBorder()` (no colour) fixes the shape but lets the border *colour* keep following the theme, while
`WithRoundedBorder(Cyan1)` pins both.

## 7. Recipe: making a new control theme-aware

1. **Store fields, not theme reads.** Give the control plain fields for each themed value (`Style`, glyph string,
   `Color`, …). `Render` reads only these fields.
2. **Add an `ApplyTheme` override** that fills those fields from `UI.StyleTheme`/`UI.GlyphTheme`, guarding each with
   `if (!IsThemeOverridden(nameof(TheProperty)))`. Call `ApplyTheme()` at the end of the constructor.
3. **Expose overridable tokens** as properties whose setters call
   `SetAtomicProperty(ref _field, value, themeOverride: true)`. Use the same property name in the `ApplyTheme`
   guard (`nameof`), which `[CallerMemberName]` guarantees to match.
4. **Route glyphs through a measure-and-resize path** (like `SetGlyphs`) if a themed glyph can change the control's
   size — never hard-code a glyph width.
5. **Do nothing else for live switching.** Subscription, the `ThemeChanged` handler, and the repaint are all on the
   base `Control`; a control that follows steps 1–4 re-themes at runtime for free.

A control that doesn't use the theme overrides nothing — the base `ApplyTheme` is a no-op, and it ignores theme
switches.

## Where things live

- Styling primitives + theme interfaces: `Jumbee.Console.Styles`
  ([Color.cs](../../src/Jumbee.Console.Styles/Color.cs), [Style.cs](../../src/Jumbee.Console.Styles/Style.cs),
  [IStyleTheme.cs](../../src/Jumbee.Console.Styles/IStyleTheme.cs),
  [IGlyphTheme.cs](../../src/Jumbee.Console.Styles/IGlyphTheme.cs), and the scrollbar/border/title types).
- Active theme + switching: [UI.cs](../../src/Jumbee.Console/UI.cs).
- Capture hook, override tracking: [Control.cs](../../src/Jumbee.Console/Control.cs),
  [ThemeOverrides.cs](../../src/Jumbee.Console/ThemeOverrides.cs).
- Themed controls: [Button.cs](../../src/Jumbee.Console/Controls/Button.cs),
  [ToggleButton.cs](../../src/Jumbee.Console/Controls/ToggleButton.cs),
  [ToggleList.cs](../../src/Jumbee.Console/Controls/ToggleList.cs),
  [ControlFrame.cs](../../src/Jumbee.Console/ControlFrame.cs).
- Interactive example: `ToggleDemo` in
  [tests/Jumbee.Console.TestDemo/Program.cs](../../tests/Jumbee.Console.TestDemo/Program.cs) — a "Switch theme
  (cool ⇄ retro)" button that re-skins glyphs, colours, frame borders, and a resized switch glyph in place.
