# <a id="Jumbee_Console_TerminalInputEvent"></a> Class TerminalInputEvent

Namespace: [Jumbee.Console](Jumbee.Console.md)  
Assembly: Jumbee.Console.dll  

Base type for the unified terminal input stream produced by <xref href="Jumbee.Console.AnsiInputDecoder" data-throw-if-not-resolved="false"></xref>: a single
sequence of key / mouse / paste / focus events, replacing the keyboard-only <xref href="System.ConsoleKeyInfo" data-throw-if-not-resolved="false"></xref> path.

```csharp
public abstract record TerminalInputEvent
```

#### Inheritance

object ← 
[TerminalInputEvent](Jumbee.Console.TerminalInputEvent.md)

#### Derived

[FocusInputEvent](Jumbee.Console.FocusInputEvent.md), 
[KeyInputEvent](Jumbee.Console.KeyInputEvent.md), 
[MouseInputEvent](Jumbee.Console.MouseInputEvent.md), 
[PasteInputEvent](Jumbee.Console.PasteInputEvent.md), 
[ResizeInputEvent](Jumbee.Console.ResizeInputEvent.md)

