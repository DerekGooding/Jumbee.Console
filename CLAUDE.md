
# About this project
The project Jumbee.Console at @src/Jumbee.Console is a .NET library for building advanced console user interfaces. It is intended to be a combination of the layout and windowing features from the retained-mode ConsoleGUI library at 
@ext/C-sharp-console-gui-framework/ConsoleGUI/ and the text styling and formatting and rendering and widget features and controls from the immediate-mode Spectre.Console library at @ext/spectre.console/src/Spectre.Console. 

The initial plan created a bridge between the two libraries by implementing `IAnsiConsole` from Spectre.Console in the `AnsiConsoleBuffer` class at @src/Jumbee.Console/AnsiConsoleBuffer.cs to store Spectre.Console control output instead of writing it to the console immediately, 
and a `SpectreControl` class for wrapping Spectre.Console controls as ConsoleGUI `IControl` to be used with ConsoleGUI control and layout classes.

`SpectreConsole` now inherits from the base `Jumbee.Console.Control` class that provides the base functionality for all Jumbee.Console controls that display output and recieve user input.
Support for updating and animating controls was added by using a single background thread started by the UI class running a timer that redraws the UI and fires Paint events at regular intervals that controls use to update
their state. Concurrent drawing conflicts are mitigated by using a single lock object that is acquired by each control derived from Control in Paint and OnInput events to synchronize access to their internal state
so that they can safely handle user input and be properly rendered. UI redraws and paint events only occur in the UI class when the lock is not held by any control. Concurrent updates to control state by multiple threads
are handled using .NET types designed for concurrent access like ConcurrentDictionary to store collections, or by using a copy-on-write strategy using the `CloneContent` and `UpdateContent` methods in the `Control` class, and by using the UI.Invoke method to acquire the UI lock when changes that affect
the global UI state and layout, like setting a Control's size, are performed.

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

- Any public properties or methods that change the visual state of a control must call the Invalidate() method to indicate that the control needs to be re-rendered and re-drawn by parent containers. 
- *Do not acquire the UI lock in publicly visible properties or methods of a control* as this will inevitably lead to deadlocks. Instead, call `Invalidate()` to signal that a control needs to be redrawn in the next Paint event.
- When modifying control state stored in collections, use .NET types designed for concurrent access like ConcurrentDictionary. For wrapping existing Spectre.Console controls, use a copy-on-write strategy using the `UpdateContent` method which invokes `CloneContent()`, to avoid modifying collections while they might be enumerated during rendering.
- Since each state change must trigger invalidation, try to batch multiple changes to control state collections into a single property or index setter with one call to Invalidate() when possible.

## Project coding instructions:
- When generating new C# code, please follow the existing coding style.
- All code should be compatible with C# 14.0.
- Prefer new C# 14.0 features and syntax where applicable.
- Prefer functional programming paradigms and constructs where appropriate.
- Prefer concise code over more verbose constructs.
- Avoid modifying external library code located in the @ext directory. Changes should be limited to the code in the @src directory only whenever possible.

## Project coding Style:
- Use the existing #regions in a file to organize class constructors, indexers, events, properties, methods, fields, and child types.
- Use 4 spaces for indentation.
- Use camel-case for method and property names. Method and property names should begin with a capital letter.
- Use camel-case for class fields. Field names should begin with lower-case letters unless they are backing fields for properties which should begin with an underscore.