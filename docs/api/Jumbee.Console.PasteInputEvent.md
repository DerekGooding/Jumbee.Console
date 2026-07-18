# <a id="Jumbee_Console_PasteInputEvent"></a> Class PasteInputEvent

Namespace: [Jumbee.Console](Jumbee.Console.md)  
Assembly: Jumbee.Console.dll  

A bracketed-paste payload, delivered as one event so it is never re-interpreted as keystrokes.

```csharp
public sealed record PasteInputEvent : TerminalInputEvent
```

#### Inheritance

object ← 
[TerminalInputEvent](Jumbee.Console.TerminalInputEvent.md) ← 
[PasteInputEvent](Jumbee.Console.PasteInputEvent.md)

## Constructors

### <a id="Jumbee_Console_PasteInputEvent__ctor_System_String_"></a> PasteInputEvent\(string\)

A bracketed-paste payload, delivered as one event so it is never re-interpreted as keystrokes.

```csharp
public PasteInputEvent(string Text)
```

#### Parameters

`Text` string

## Properties

### <a id="Jumbee_Console_PasteInputEvent_Text"></a> Text

```csharp
public string Text { get; init; }
```

#### Property Value

 string

