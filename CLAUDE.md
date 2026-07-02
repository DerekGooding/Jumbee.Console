
# About this project
The project Jumbee.Console at @src/Jumbee.Console is a .NET library for building advanced console user interfaces. It is intended to be a combination of the layout and windowing features from the retained-mode ConsoleGUI library at 
@ext/C-sharp-console-gui-framework/ConsoleGUI/ and the text styling and formatting and rendering and widget features and controls from the immediate-mode Spectre.Console library at @ext/spectre.console/src/Spectre.Console. 

The initial plan created a bridge between the two libraries by implementing `IAnsiConsole` from Spectre.Console in the `AnsiConsoleBuffer` class at @src/Jumbee.Console/AnsiConsoleBuffer.cs to store Spectre.Console control output instead of writing it to the console immediately, 
and a `SpectreControl` class for wrapping Spectre.Console controls as ConsoleGUI `IControl` to be used with ConsoleGUI control and layout classes.

`SpectreConsole` now inherits from the base `Jumbee.Console.Control` class that provides the base functionality for all Jumbee.Console controls that display output and recieve user input.
Support for updating and animating controls is provided by the `UI` class, which runs a single dedicated UI thread via the `Dispatcher`. Each frame the dispatcher drains a work queue, dispatches input, and redraws the UI, firing Paint events that controls use to render their state. All UI state mutation and rendering happen on this one UI thread, so there is no shared lock. Code on other threads marshals work onto the UI thread via `UI.Invoke` (runs inline when already on the UI thread, otherwise posts to the dispatcher queue), `UI.Post`, or `UI.InvokeAsync`. Atomic scalar property changes (via `SetAtomicProperty`) are written directly and may briefly tear for multi-field structs (a self-correcting one-frame risk that is accepted); non-atomic changes (collections, or mutating a wrapped Spectre control) are marshaled onto the UI thread via `UI.Invoke` so they never race with rendering. Layout/geometry changes (such as setting a Control's size) also go through `UI.Invoke` so they run on the UI thread before the next redraw. The UI thread installs a `SynchronizationContext`, so `await` inside code already running on the UI thread (event handlers, the `onUpdate` callback, anything started via `UI.Invoke`/`Post`/`InvokeAsync`) resumes back on the UI thread — meaning a background `await SomethingAsync()` can be followed by direct control updates with no explicit marshaling (the WPF/Avalonia model).

Control layout is handled using `Jumbee.Console.Layout` derived classes that wrap ConsoleGUI layout classes. Drawing conflicts from concurrent updates in ConsoleGUI layout classes are mitigated using the UI.Invoke method 
to synchronize concurrent requests for changes to a layout control before redrawing and propagating the changes upward to parent containers.

## Project design and architecture

Read all the markdown docs at @docs/internal/*.md to understand the integration between Console.GUI and Spectre.Console in Jumbee.Console.

### Control class
Controls are represented by the common Jumbee.Console.Control class. 

### ControlFrame class
A Control can have an optional ControlFrame object in its Frame property. The Jumbee.Console.ControlFrame class has a single Jumbee.Console.Control as its child and draws borders, margins, scrollbars, a titlebar, and other control adornments around its child control, combining the drawing logic
of ConsoleGUI classes like Border, Margin, and VerticalScrollPanel.

### Layout class
Controls and ControlFrames can be placed in Jumbee.Console.Layout classes for arrangement. This class wraps existing ConsoleGUI layout controls like ConsoleGUI.Controls.Grid.

### RenderableControl class
The RenderableControl implements Spectre.Console.IRenderable and is designed for new control implementations that want to use the Spectre.Console text styling and layout and rendering
features. It uses an AnsiConsoleBuffer to render to a ConsoleBuffer which is used by ConsoleGUI to draw the control to the console screen.

### SpectreControl class
The SpectreControl class is a generic class that wraps an existing Spectre.Console IRenderable control as a ConsoleGUI IControl. It uses the AnsiConsoleBuffer to render the Spectre control to a buffer, 

### Control implementation considerations
Note the following important considerations when deriving from these classes:

- Any public property or method that changes the visual state of a control must request a redraw: use `SetAtomicProperty` for simple scalar properties (it does the equality check, assign, and invalidate), 
- or call `Invalidate()` directly. `Invalidate()` marks the control dirty so it is re-rendered on the next frame.
- All UI state and rendering live on the single UI thread; there is no UI lock. Do not add locks for UI state. To mutate a control from a background thread, marshal the change onto the UI thread with `UI.Invoke` (inline if already on the UI thread, else posted), `UI.Post`, or `UI.InvokeAsync`.
- Atomic scalar properties may be written directly (via `SetAtomicProperty`), accepting the rare one-frame tear for multi-field structs. Non-atomic mutations (collections, or mutating a wrapped Spectre control via `UpdateContent`) must be marshaled onto the UI thread with `UI.Invoke`; this is what lets controls use a plain `Dictionary`/`List` instead of concurrent collections. Reads of collection state should likewise happen on the UI thread.
- Batch multiple state changes into a single property or index setter with one invalidation when possible.

## Project coding instructions:
- When generating new C# code, please follow the existing coding style.
- All code should be compatible with .NET 10.0 / C# 14.0.
- Prefer new C# 14.0 features and syntax where applicable.
- Prefer functional programming paradigms and constructs where appropriate.
- Prefer concise code over more verbose constructs.
- Avoid modifying external library code located in the @ext directory. Changes should be limited to the code in the @src directory only whenever possible.

## Project coding style:
- Use the existing #regions in a file to organize class constructors, indexers, events, properties, methods, fields, and child types.
- Use 4 spaces for indentation.
- Use camel-case for method and property names. Method and property names should begin with a capital letter.
- Use camel-case for class fields. Field names should begin with lower-case letters unless they are backing fields for properties which should begin with an underscore.
- Group members with the same visibility together. The reading order should be public -> internal -> protected -> private.

## Project documentation style
- Avoid verbose documentation on members. Try to be as terse as possible while giving all relevant information about usage. Avoid mentioning other TUI libraries unless it is relevant to the usage of the class or member.