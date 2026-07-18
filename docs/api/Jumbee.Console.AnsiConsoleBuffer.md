# <a id="Jumbee_Console_AnsiConsoleBuffer"></a> Class AnsiConsoleBuffer

Namespace: [Jumbee.Console](Jumbee.Console.md)  
Assembly: Jumbee.Console.dll  

An implementation of Spectre.Console.IAnsiConsole that writes to a ConsoleBuffer.

```csharp
public class AnsiConsoleBuffer
```

#### Inheritance

object ← 
[AnsiConsoleBuffer](Jumbee.Console.AnsiConsoleBuffer.md)

## Constructors

### <a id="Jumbee_Console_AnsiConsoleBuffer__ctor_Jumbee_Console_ConsoleBuffer_"></a> AnsiConsoleBuffer\(ConsoleBuffer\)

Initializes a new <xref href="Jumbee.Console.AnsiConsoleBuffer" data-throw-if-not-resolved="false"></xref> that renders Spectre.Console output into
    <code class="paramref">console</code>.

```csharp
public AnsiConsoleBuffer(ConsoleBuffer console)
```

#### Parameters

`console` [ConsoleBuffer](Jumbee.Console.ConsoleBuffer.md)

## Fields

### <a id="Jumbee_Console_AnsiConsoleBuffer_marshal"></a> marshal

When <a href="https://learn.microsoft.com/dotnet/csharp/language-reference/builtin-types/bool">true</a>, <xref href="Jumbee.Console.AnsiConsoleBuffer.Write(Spectre.Console.Rendering.IRenderable)" data-throw-if-not-resolved="false"></xref> and <xref href="Jumbee.Console.AnsiConsoleBuffer.Clear(System.Boolean)" data-throw-if-not-resolved="false"></xref> are marshaled onto the UI thread
via <xref href="Jumbee.Console.UI.Invoke(System.Action)" data-throw-if-not-resolved="false"></xref> so their buffer mutations are serialized with rendering and resizing.

```csharp
public bool marshal
```

#### Field Value

 bool

#### Remarks

Set this for controls whose wrapped Spectre widget refreshes from its own thread (e.g.
<xref href="Jumbee.Console.SpectreLiveDisplay" data-throw-if-not-resolved="false"></xref>, <xref href="Jumbee.Console.SpectreTaskProgress" data-throw-if-not-resolved="false"></xref>). Defaults to <a href="https://learn.microsoft.com/dotnet/csharp/language-reference/builtin-types/bool">false</a> to
preserve the original synchronous IAnsiConsole behavior for existing Spectre.Console controls.

### <a id="Jumbee_Console_AnsiConsoleBuffer_wrap"></a> wrap

When <a href="https://learn.microsoft.com/dotnet/csharp/language-reference/builtin-types/bool">true</a>, <xref href="Jumbee.Console.AnsiConsoleBuffer.Write(Spectre.Console.Rendering.IRenderable)" data-throw-if-not-resolved="false"></xref> wraps glyphs to the next row at the buffer's
    right edge instead of clipping them. See the wrap note in <xref href="Jumbee.Console.AnsiConsoleBuffer._Write(System.Collections.Generic.IReadOnlyList%7bSpectre.Console.Rendering.Segment%7d)" data-throw-if-not-resolved="false"></xref>.

```csharp
public bool wrap
```

#### Field Value

 bool

## Properties

### <a id="Jumbee_Console_AnsiConsoleBuffer_Cursor"></a> Cursor

The cursor for this buffer.

```csharp
public IAnsiConsoleCursor Cursor { get; }
```

#### Property Value

 IAnsiConsoleCursor

### <a id="Jumbee_Console_AnsiConsoleBuffer_CursorDistance"></a> CursorDistance

The cursor position expressed as a linear (row-major) cell distance from the buffer origin.

```csharp
public int CursorDistance { get; set; }
```

#### Property Value

 int

### <a id="Jumbee_Console_AnsiConsoleBuffer_ExclusivityMode"></a> ExclusivityMode

The exclusivity mode guarding concurrent Spectre live/exclusive operations.

```csharp
public IExclusivityMode ExclusivityMode { get; }
```

#### Property Value

 IExclusivityMode

### <a id="Jumbee_Console_AnsiConsoleBuffer_Input"></a> Input

The input source for this console (throws on read; input is handled elsewhere).

```csharp
public IAnsiConsoleInput Input { get; }
```

#### Property Value

 IAnsiConsoleInput

### <a id="Jumbee_Console_AnsiConsoleBuffer_Pipeline"></a> Pipeline

The Spectre.Console render pipeline for this console.

```csharp
public RenderPipeline Pipeline { get; }
```

#### Property Value

 RenderPipeline

### <a id="Jumbee_Console_AnsiConsoleBuffer_Profile"></a> Profile

The Spectre.Console profile describing this buffer's capabilities.

```csharp
public Profile Profile { get; }
```

#### Property Value

 Profile

## Methods

### <a id="Jumbee_Console_AnsiConsoleBuffer_Clear_System_Boolean_"></a> Clear\(bool\)

Clears the buffer; when <code class="paramref">home</code> is <a href="https://learn.microsoft.com/dotnet/csharp/language-reference/builtin-types/bool">true</a> also resets the cursor to the origin.

```csharp
public void Clear(bool home)
```

#### Parameters

`home` bool

### <a id="Jumbee_Console_AnsiConsoleBuffer_Dispose"></a> Dispose\(\)

Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.

```csharp
public void Dispose()
```

### <a id="Jumbee_Console_AnsiConsoleBuffer_Write_Spectre_Console_Rendering_IRenderable_"></a> Write\(IRenderable\)

Renders <code class="paramref">renderable</code> to segments and writes them into the buffer.

```csharp
public void Write(IRenderable renderable)
```

#### Parameters

`renderable` IRenderable

### <a id="Jumbee_Console_AnsiConsoleBuffer_Write_System_Collections_Generic_IReadOnlyList_Spectre_Console_Rendering_Segment__"></a> Write\(IReadOnlyList<Segment\>\)

Applies pre-rendered segments to the buffer, bypassing markup parsing and <xref href="Spectre.Console.Rendering.IRenderable" data-throw-if-not-resolved="false"></xref>
rendering.

```csharp
public void Write(IReadOnlyList<Segment> segments)
```

#### Parameters

`segments` IReadOnlyList<Segment\>

#### Remarks

Used by syntax highlighters (see <code>SpectreSegmentFormatter</code>) that emit styled <xref href="Spectre.Console.Rendering.Segment" data-throw-if-not-resolved="false"></xref>s
directly. Honours <xref href="Jumbee.Console.AnsiConsoleBuffer.marshal" data-throw-if-not-resolved="false"></xref> and <xref href="Jumbee.Console.AnsiConsoleBuffer.wrap" data-throw-if-not-resolved="false"></xref> like <xref href="Jumbee.Console.AnsiConsoleBuffer.Write(Spectre.Console.Rendering.IRenderable)" data-throw-if-not-resolved="false"></xref>.

