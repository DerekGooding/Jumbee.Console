# <a id="Jumbee_Console_IInputSource"></a> Interface IInputSource

Namespace: [Jumbee.Console](Jumbee.Console.md)  
Assembly: Jumbee.Console.dll  

Supplies <xref href="Jumbee.Console.TerminalInputEvent" data-throw-if-not-resolved="false"></xref>s (keys, mouse, paste, focus) to the UI input loop.

```csharp
public interface IInputSource
```

## Remarks

The default reads the real console; tests (or scripted/headless scenarios) can supply their own to inject events
deterministically.

## Methods

### <a id="Jumbee_Console_IInputSource_TryRead_Jumbee_Console_TerminalInputEvent__"></a> TryRead\(out TerminalInputEvent?\)

Returns the next available input event without blocking. Returns <a href="https://learn.microsoft.com/dotnet/csharp/language-reference/builtin-types/bool">false</a> when none is ready.

```csharp
bool TryRead(out TerminalInputEvent? evt)
```

#### Parameters

`evt` [TerminalInputEvent](Jumbee.Console.TerminalInputEvent.md)?

#### Returns

 bool

