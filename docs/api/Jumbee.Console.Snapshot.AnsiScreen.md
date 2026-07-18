# <a id="Jumbee_Console_Snapshot_AnsiScreen"></a> Class AnsiScreen

Namespace: [Jumbee.Console.Snapshot](Jumbee.Console.Snapshot.md)  
Assembly: Jumbee.Console.Snapshot.dll  

A small VT/ANSI screen model that parses the subset of escape sequences <code>ConsoleManager</code> emits and
maintains a cell grid, exactly as a terminal would.

```csharp
public sealed class AnsiScreen
```

#### Inheritance

object ← 
[AnsiScreen](Jumbee.Console.Snapshot.AnsiScreen.md)

## Remarks

<p>
The parsed subset covers CUP cursor moves, 24-bit SGR foreground/background (and default 39/49), the SGR
decorations, cursor visibility (DECTCEM <code>?25</code>), and OSC strings. Feed it the bytes captured from
<code>ConsoleManager.AnsiOutput</code> and read the resulting <xref href="Jumbee.Console.Snapshot.AnsiScreen.Buffer" data-throw-if-not-resolved="false"></xref>.
</p>
<p>
It is deliberately a <em>faithful</em> emulator of the emitted subset (SGR attributes accumulate; <code>ESC[m</code>
/ <code>ESC[0m</code> resets foreground, background, and decorations), so an encoding bug in the render path — a
colour that should have been re-emitted but wasn't, a cell that wasn't erased — shows up as a wrong cell here.
</p>

## Constructors

### <a id="Jumbee_Console_Snapshot_AnsiScreen__ctor_System_Int32_System_Int32_"></a> AnsiScreen\(int, int\)

Initializes a new <xref href="Jumbee.Console.Snapshot.AnsiScreen" data-throw-if-not-resolved="false"></xref> with a blank <code class="paramref">width</code>×<code class="paramref">height</code> cell grid.

```csharp
public AnsiScreen(int width, int height)
```

#### Parameters

`width` int

`height` int

## Properties

### <a id="Jumbee_Console_Snapshot_AnsiScreen_Buffer"></a> Buffer

The parsed screen image. Reuse <xref href="Jumbee.Console.Snapshot.ConsoleSnapshot.ToText(Jumbee.Console.ConsoleBuffer)" data-throw-if-not-resolved="false"></xref> to read it.

```csharp
public ConsoleBuffer Buffer { get; }
```

#### Property Value

 [ConsoleBuffer](Jumbee.Console.ConsoleBuffer.md)

### <a id="Jumbee_Console_Snapshot_AnsiScreen_CursorPosition"></a> CursorPosition

The cursor position (0-based) as last set by a CUP move.

```csharp
public Position CursorPosition { get; }
```

#### Property Value

 Position

### <a id="Jumbee_Console_Snapshot_AnsiScreen_CursorVisible"></a> CursorVisible

Whether the terminal cursor is currently shown (last DECTCEM <code>?25h</code>/<code>?25l</code>).

```csharp
public bool CursorVisible { get; }
```

#### Property Value

 bool

## Methods

### <a id="Jumbee_Console_Snapshot_AnsiScreen_Feed_System_String_"></a> Feed\(string\)

Parses <code class="paramref">ansi</code> and applies it to the screen (cumulative across calls).

```csharp
public void Feed(string ansi)
```

#### Parameters

`ansi` string

