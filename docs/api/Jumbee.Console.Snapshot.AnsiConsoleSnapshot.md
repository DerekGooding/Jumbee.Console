# <a id="Jumbee_Console_Snapshot_AnsiConsoleSnapshot"></a> Class AnsiConsoleSnapshot

Namespace: [Jumbee.Console.Snapshot](Jumbee.Console.Snapshot.md)  
Assembly: Jumbee.Console.Snapshot.dll  

Drives the <em>real</em> <xref href="ConsoleGUI.ConsoleManager" data-throw-if-not-resolved="false"></xref> ANSI render path headlessly, captures the emitted escape
sequences via <xref href="ConsoleGUI.ConsoleManager.AnsiOutput" data-throw-if-not-resolved="false"></xref>, and parses them back into an <xref href="Jumbee.Console.Snapshot.AnsiScreen" data-throw-if-not-resolved="false"></xref>.

```csharp
public static class AnsiConsoleSnapshot
```

#### Inheritance

object ← 
[AnsiConsoleSnapshot](Jumbee.Console.Snapshot.AnsiConsoleSnapshot.md)

## Remarks

<p>
Where <xref href="Jumbee.Console.Snapshot.ConsoleSnapshot" data-throw-if-not-resolved="false"></xref> composes the logical cell grid through a DrawingContext, this exercises
ConsoleManager's actual ANSI encoding, diff, cursor handling, and serialized async output — the path that
previously could only be checked against a real terminal.
</p>
<p>
It mutates <xref href="ConsoleGUI.ConsoleManager" data-throw-if-not-resolved="false"></xref> global state, so a caller must ensure no UI loop is concurrently driving
it (the test suite disables parallelization and stops the UI first). The capture sink writes on a thread-pool
thread like production; <xref href="ConsoleGUI.ConsoleManager.OutputIdle" data-throw-if-not-resolved="false"></xref> is awaited so the result is deterministic.
</p>

## Methods

### <a id="Jumbee_Console_Snapshot_AnsiConsoleSnapshot_RenderAsync_ConsoleGUI_IControl_System_Int32_System_Int32_"></a> RenderAsync\(IControl, int, int\)

Renders <code class="paramref">content</code> through ConsoleManager's ANSI path and returns the parsed screen.

```csharp
public static Task<AnsiScreen> RenderAsync(IControl content, int width, int height)
```

#### Parameters

`content` IControl

`width` int

`height` int

#### Returns

 Task<[AnsiScreen](Jumbee.Console.Snapshot.AnsiScreen.md)\>

### <a id="Jumbee_Console_Snapshot_AnsiConsoleSnapshot_RenderAsync_Jumbee_Console_Control_System_Int32_System_Int32_"></a> RenderAsync\(Control, int, int\)

Renders a Jumbee control (using its frame when present) through the ANSI path.

```csharp
public static Task<AnsiScreen> RenderAsync(Control control, int width, int height)
```

#### Parameters

`control` [Control](Jumbee.Console.Control.md)

`width` int

`height` int

#### Returns

 Task<[AnsiScreen](Jumbee.Console.Snapshot.AnsiScreen.md)\>

### <a id="Jumbee_Console_Snapshot_AnsiConsoleSnapshot_RenderAsync_Jumbee_Console_ILayout_System_Int32_System_Int32_"></a> RenderAsync\(ILayout, int, int\)

Renders a layout through the ANSI path.

```csharp
public static Task<AnsiScreen> RenderAsync(ILayout layout, int width, int height)
```

#### Parameters

`layout` [ILayout](Jumbee.Console.ILayout.md)

`width` int

`height` int

#### Returns

 Task<[AnsiScreen](Jumbee.Console.Snapshot.AnsiScreen.md)\>

### <a id="Jumbee_Console_Snapshot_AnsiConsoleSnapshot_ToTextAsync_Jumbee_Console_Control_System_Int32_System_Int32_"></a> ToTextAsync\(Control, int, int\)

Renders a control through the ANSI path and returns the parsed screen as text (glyphs only).

```csharp
public static Task<string> ToTextAsync(Control control, int width, int height)
```

#### Parameters

`control` [Control](Jumbee.Console.Control.md)

`width` int

`height` int

#### Returns

 Task<string\>

### <a id="Jumbee_Console_Snapshot_AnsiConsoleSnapshot_ToTextAsync_Jumbee_Console_ILayout_System_Int32_System_Int32_"></a> ToTextAsync\(ILayout, int, int\)

Renders a layout through the ANSI path and returns the parsed screen as text (glyphs only).

```csharp
public static Task<string> ToTextAsync(ILayout layout, int width, int height)
```

#### Parameters

`layout` [ILayout](Jumbee.Console.ILayout.md)

`width` int

`height` int

#### Returns

 Task<string\>

