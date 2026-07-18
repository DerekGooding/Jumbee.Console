# ConsoleGUI and Spectre.Console Control Integration

This document outlines the architectural pattern for integrating `Spectre.Console` widgets into the ConsoleGUI framework. It specifically addresses how the layout and rendering pipelines of the two libraries bridge via the Jumbee.Console `SpectreControl` and `AnsiConsoleBuffer` classes.

## 1. The Integration Challenge

*   **ConsoleGUI** uses a **pull-based** layout system. Controls are initialized with constraints (`MinSize`, `MaxSize`) and must determine their own `Size` during an `Initialize()` pass.
*   **Spectre.Console** uses a **rendering pipeline** where objects are typically `Measure`d and then `Render`ed into segments.

To host a Spectre widget (like a `Panel`, `Table`, or `BarChart`) inside the ConsoleGUI layout system, we must translate ConsoleGUI's layout constraints into a Spectre `Measure` call, and then capture the Spectre `Render` output into a buffer that ConsoleGUI can display.

## 2. The Bridge Components

### 2.1 `SpectreControl<T>`
This generic class wraps a `Spectre.Console.IRenderable`. It inherits from `Jumbee.Console.RenderableControl` (itself a `Jumbee.Console.Control`) and acts as the adapter.

### 2.2 `AnsiConsoleBuffer`
This class implements `Spectre.Console.IAnsiConsole` but directs output to a ConsoleGUI `ConsoleBuffer` (a 2D character array) instead of the standard output.

## 3. The Layout Process: `Initialize()`

The critical integration point is the `Initialize()` method of `Jumbee.Console.Control`.

### The Base Implementation (`Control`)
The base `Control` class provides a default implementation that essentially fills the available space or defaults to a safe maximum if unconstrained.

```csharp
protected override void Initialize()
{
    // Default logic: Set size to MaxSize (clamped)
    var size = ...; 
    Resize(size);
    // ...
}
```

### The `RenderableControl` Implementation
For proper layout, `RenderableControl` (the base of `SpectreControl<T>`) **overrides** `Initialize()` and calls `Measure()` to determine the optimal size, resizing only when the computed size actually changed. `SpectreControl<T>` implements `Measure()` by delegating to the wrapped Spectre widget.
> **Note:** Spectre's `Measure` method primarily returns width. Calculating the exact height often requires a dry-run `Render` or specific knowledge of the widget. `SpectreControl` should ideally perform a layout-only render if height is unknown and critical.

## 4. The Rendering Process: `Render()`

The parameterless `Render()` method in `RenderableControl` bridges the drawing phase.

1.  **Clear**: The `AnsiConsoleBuffer` is cleared to ensure no artifacts remain from the previous frame.
2.  **Write**: The `Content` (Spectre renderable) is written to the `ansiConsole`.
3.  **Pipeline**:
    *   `ansiConsole.Write(Content)` -> `RenderPipeline` -> `Content.Render(...)` -> `Segments`
    *   The `AnsiConsoleBuffer` interprets the resulting ANSI sequences/Segments and plots `Cell`s (Char + Color) onto the `ConsoleBuffer`.

```csharp
protected sealed override void Render()
{  
    // Retained-mode fast path: only re-runs the Spectre pipeline when content/size/theme changed.

    // 1. Clear the virtual console buffer
    ansiConsole.Clear(true); 
    
    // 2. Render this control (an IRenderable) into the buffer; the segment-producing
    //    Render(options, maxWidth) override delegates to the wrapped content.
    ansiConsole.Write(this);        
}
```

## 5. State Management and Threading

*   **Invalidate**: When a property on the wrapped Spectre control changes (e.g., `Panel.Header = ...`), the `SpectreControl` wrapper must call `Invalidate()`. This schedules a repaint on the UI thread.
*   **Content mutation**: `Spectre.Console` widgets are often modified in place. All UI state and rendering run on the single UI thread, so a non-atomic mutation of the wrapped content must be marshaled there via `SpectreControl.UpdateContent(Action<T>)`, which applies the change on the UI thread so it never races with rendering. (This replaced the earlier copy-on-write / `CloneContent` approach.)

## 6. Summary of Responsibilities

| Component | Responsibility |
| :--- | :--- |
| **ConsoleGUI Parent** | Sets `MinSize`/`MaxSize` on the `SpectreControl` via `Context`. Calls `Initialize()`. |
| **RenderableControl / SpectreControl** | `RenderableControl` overrides `Initialize()` (calling `Measure()` and `Resize()`) and the parameterless `Render()`; `SpectreControl` supplies `Measure()` and the segment-producing `Render(options, maxWidth)` by delegating to the wrapped content. |
| **AnsiConsoleBuffer** | Intercepts Spectre output and maps (X,Y) coordinates to the `ConsoleBuffer`. |
| **Spectre Widget** | Provides `Measure()` logic and generates `Segment`s during `Render`. |
