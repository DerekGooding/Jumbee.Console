# <a id="Jumbee_Console_ConsoleInputSource"></a> Class ConsoleInputSource

Namespace: [Jumbee.Console](Jumbee.Console.md)  
Assembly: Jumbee.Console.dll  

The default <xref href="Jumbee.Console.IInputSource" data-throw-if-not-resolved="false"></xref>, reading keys from <xref href="System.Console" data-throw-if-not-resolved="false"></xref> and wrapping them as
<xref href="Jumbee.Console.KeyInputEvent" data-throw-if-not-resolved="false"></xref>s.

```csharp
public sealed class ConsoleInputSource : IInputSource
```

#### Inheritance

object ← 
[ConsoleInputSource](Jumbee.Console.ConsoleInputSource.md)

#### Implements

[IInputSource](Jumbee.Console.IInputSource.md)

## Remarks

Returns no event when console input is redirected or unavailable. (Mouse/paste/focus require the raw VT input
source — a later step; this keyboard-only source keeps existing behavior.)

## Methods

### <a id="Jumbee_Console_ConsoleInputSource_TryRead_Jumbee_Console_TerminalInputEvent__"></a> TryRead\(out TerminalInputEvent?\)

Returns the next available input event without blocking. Returns <a href="https://learn.microsoft.com/dotnet/csharp/language-reference/builtin-types/bool">false</a> when none is ready.

```csharp
public bool TryRead(out TerminalInputEvent? evt)
```

#### Parameters

`evt` [TerminalInputEvent](Jumbee.Console.TerminalInputEvent.md)?

#### Returns

 bool

