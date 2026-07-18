# <a id="Jumbee_Console_MouseInputEvent"></a> Class MouseInputEvent

Namespace: [Jumbee.Console](Jumbee.Console.md)  
Assembly: Jumbee.Console.dll  

A mouse action at cell coordinates (0-based).

```csharp
public sealed record MouseInputEvent : TerminalInputEvent
```

#### Inheritance

object ← 
[TerminalInputEvent](Jumbee.Console.TerminalInputEvent.md) ← 
[MouseInputEvent](Jumbee.Console.MouseInputEvent.md)

## Constructors

### <a id="Jumbee_Console_MouseInputEvent__ctor_System_Int32_System_Int32_Jumbee_Console_TerminalMouseButton_Jumbee_Console_TerminalMouseKind_Jumbee_Console_TerminalModifiers_"></a> MouseInputEvent\(int, int, TerminalMouseButton, TerminalMouseKind, TerminalModifiers\)

A mouse action at cell coordinates (0-based).

```csharp
public MouseInputEvent(int X, int Y, TerminalMouseButton Button, TerminalMouseKind Kind, TerminalModifiers Modifiers)
```

#### Parameters

`X` int

`Y` int

`Button` [TerminalMouseButton](Jumbee.Console.TerminalMouseButton.md)

`Kind` [TerminalMouseKind](Jumbee.Console.TerminalMouseKind.md)

`Modifiers` [TerminalModifiers](Jumbee.Console.TerminalModifiers.md)

## Properties

### <a id="Jumbee_Console_MouseInputEvent_Button"></a> Button

```csharp
public TerminalMouseButton Button { get; init; }
```

#### Property Value

 [TerminalMouseButton](Jumbee.Console.TerminalMouseButton.md)

### <a id="Jumbee_Console_MouseInputEvent_Kind"></a> Kind

```csharp
public TerminalMouseKind Kind { get; init; }
```

#### Property Value

 [TerminalMouseKind](Jumbee.Console.TerminalMouseKind.md)

### <a id="Jumbee_Console_MouseInputEvent_Modifiers"></a> Modifiers

```csharp
public TerminalModifiers Modifiers { get; init; }
```

#### Property Value

 [TerminalModifiers](Jumbee.Console.TerminalModifiers.md)

### <a id="Jumbee_Console_MouseInputEvent_X"></a> X

```csharp
public int X { get; init; }
```

#### Property Value

 int

### <a id="Jumbee_Console_MouseInputEvent_Y"></a> Y

```csharp
public int Y { get; init; }
```

#### Property Value

 int

