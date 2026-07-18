# <a id="Jumbee_Console_VtInputSource"></a> Class VtInputSource

Namespace: [Jumbee.Console](Jumbee.Console.md)  
Assembly: Jumbee.Console.dll  

An <xref href="Jumbee.Console.IInputSource" data-throw-if-not-resolved="false"></xref> that puts the terminal into VT input mode and enables mouse, bracketed-paste, and
focus reporting, then reads the raw stdin byte stream, decodes it (UTF-8) and runs it through
<xref href="Jumbee.Console.AnsiInputDecoder" data-throw-if-not-resolved="false"></xref> to produce <xref href="Jumbee.Console.TerminalInputEvent" data-throw-if-not-resolved="false"></xref>s.

```csharp
public sealed class VtInputSource : IInputSource
```

#### Inheritance

object ← 
[VtInputSource](Jumbee.Console.VtInputSource.md)

#### Implements

[IInputSource](Jumbee.Console.IInputSource.md)

## Remarks

<p>
Restores all terminal state on <xref href="Jumbee.Console.VtInputSource.Dispose" data-throw-if-not-resolved="false"></xref> — pass one to <xref href="Jumbee.Console.UI.Start(Jumbee.Console.ILayout%2cSystem.Int32%2cSystem.Int32%2cSystem.Int32%2cSystem.Boolean%2cConsoleGUI.Api.IConsole%2cJumbee.Console.IInputSource%2cSystem.Boolean)" data-throw-if-not-resolved="false"></xref> and it is disposed by
<xref href="Jumbee.Console.UI.Stop" data-throw-if-not-resolved="false"></xref>.
</p>
<p>
Reading runs on a dedicated background thread holding a single outstanding read; an idle timeout flushes the
decoder so a lone ESC keypress resolves to <xref href="System.ConsoleKey.Escape" data-throw-if-not-resolved="false"></xref> instead of waiting for the next byte.
Requires a real interactive terminal; not used by the headless/test paths (which inject their own source).
</p>

## Constructors

### <a id="Jumbee_Console_VtInputSource__ctor_System_Int32_System_Boolean_"></a> VtInputSource\(int, bool\)

```csharp
public VtInputSource(int idleFlushMs = 40, bool anyMotion = false)
```

#### Parameters

`idleFlushMs` int

Idle timeout before flushing a dangling escape sequence.

`anyMotion` bool

When <a href="https://learn.microsoft.com/dotnet/csharp/language-reference/builtin-types/bool">true</a>, request any-motion mouse tracking (DEC 1003) so the pointer is reported on
every move (enabling hover), instead of only while a button is held (DEC 1002). Costs more input traffic.

## Methods

### <a id="Jumbee_Console_VtInputSource_Dispose"></a> Dispose\(\)

Stops the reader thread and restores the console mode (disabling mouse/paste/focus reporting).

```csharp
public void Dispose()
```

### <a id="Jumbee_Console_VtInputSource_TryRead_Jumbee_Console_TerminalInputEvent__"></a> TryRead\(out TerminalInputEvent?\)

Returns the next available input event without blocking. Returns <a href="https://learn.microsoft.com/dotnet/csharp/language-reference/builtin-types/bool">false</a> when none is ready.

```csharp
public bool TryRead(out TerminalInputEvent? evt)
```

#### Parameters

`evt` [TerminalInputEvent](Jumbee.Console.TerminalInputEvent.md)?

#### Returns

 bool

