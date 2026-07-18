# <a id="Jumbee_Console_FocusInputEvent"></a> Class FocusInputEvent

Namespace: [Jumbee.Console](Jumbee.Console.md)  
Assembly: Jumbee.Console.dll  

Terminal focus gained/lost (DEC mode 1004).

```csharp
public sealed record FocusInputEvent : TerminalInputEvent
```

#### Inheritance

object ← 
[TerminalInputEvent](Jumbee.Console.TerminalInputEvent.md) ← 
[FocusInputEvent](Jumbee.Console.FocusInputEvent.md)

## Constructors

### <a id="Jumbee_Console_FocusInputEvent__ctor_System_Boolean_"></a> FocusInputEvent\(bool\)

Terminal focus gained/lost (DEC mode 1004).

```csharp
public FocusInputEvent(bool HasFocus)
```

#### Parameters

`HasFocus` bool

## Properties

### <a id="Jumbee_Console_FocusInputEvent_HasFocus"></a> HasFocus

```csharp
public bool HasFocus { get; init; }
```

#### Property Value

 bool

