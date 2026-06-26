# Composite Controls

A **composite control** is a single `Control` built out of several child controls laid out together — for
example a `CodeEditor` (a `TextEditor` with a line-number gutter docked to its left). Because it *is* a
`Control`, it drops into any layout cell, can be framed, and participates in theming and painting like any leaf
control.

This is what `Layout` cannot give you: a `Layout` arranges independent top-level controls, but it is not a
`Control`, so you can't subclass it into a reusable widget. `CompositeControl` fills that gap.

## Using one: `CodeEditor`

```csharp
var editor = new CodeEditor(Language.CSharp) { Text = File.ReadAllText("Program.cs") };
editor.WithRoundedBorder(Color.Cyan1).WithTitle("Program.cs");

var grid = new Grid([20], [80], [[editor]]);
var run = UI.Start(grid, width: 84, height: 22, input: new VtInputSource(anyMotion: true));
UI.SetFocus(editor.Editor);   // focus the inner editor to type
run.Wait();
```

The gutter automatically tracks the editor's line count and highlights the caret's line — it reacts to the
editor's `Changed` event. `editor.Editor` and `editor.Gutter` expose the children.

> **Scrolling (current limitation):** the inner editor isn't independently scrolled inside the composite yet, so
> content taller than the viewport won't scroll. Keep content within the visible area for now; scroll-sync is a
> planned follow-up.

## Authoring a composite: subclass `CompositeControl`

Build the children, arrange them with any `Layout` (here a `DockPanel`), wire their interactions, then call
`SetContent`:

```csharp
public class LabeledInput : CompositeControl
{
    private readonly TextLabel _label;
    private readonly TextEditor _input;

    public LabeledInput(string caption)
    {
        _label = new TextLabel(TextLabelOrientation.Horizontal, caption, Color.Grey) { Focusable = false };
        _input = new TextEditor();

        // Inter-child behaviour goes here — just subscribe to the children's events:
        _input.Changed += (_, _) => { /* e.g. validate, update the label … */ };

        SetContent(new DockPanel(DockedControlPlacement.Left, _label, _input));
    }

    public TextEditor Input => _input;
}
```

What you get for free:

- **Layout** — reuse `Grid`, `DockPanel`, `HorizontalStackPanel`, etc. as the internal arrangement.
- **Rendering & mouse** — each child renders itself; the composite composites their cells (with the child's own
  mouse listener intact), so hover/click and click-to-focus reach the children automatically.
- **Keyboard** — `CompositeControl` overrides `FocusedControl` to return the focused descendant, so keystrokes
  from the parent layout route to the right child.
- **Inter-child communication** — the composite owns the children as fields, so you wire their events directly in
  the constructor. No event bus or mediator is needed.

Notes:

- Mark pure adornments (labels, gutters) `Focusable = false` so they stay out of the tab order and don't take
  clicks.
- The composite paints nothing of its own by default. Override `Render()` to draw a background or separators into
  the buffer *behind* the children.
- Expose the children you want callers to drive (e.g. `Editor`, `Input`) as properties.

See `CodeEditorDemo` in the TestDemo project for a runnable example.
