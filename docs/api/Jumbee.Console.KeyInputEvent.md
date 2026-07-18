# <a id="Jumbee_Console_KeyInputEvent"></a> Class KeyInputEvent

Namespace: [Jumbee.Console](Jumbee.Console.md)  
Assembly: Jumbee.Console.dll  

A key press.

```csharp
public sealed record KeyInputEvent : TerminalInputEvent
```

#### Inheritance

object ← 
[TerminalInputEvent](Jumbee.Console.TerminalInputEvent.md) ← 
[KeyInputEvent](Jumbee.Console.KeyInputEvent.md)

## Remarks

Bridges to/from the existing <xref href="ConsoleGUI.Input.InputEvent" data-throw-if-not-resolved="false"></xref> path via <xref href="System.ConsoleKeyInfo" data-throw-if-not-resolved="false"></xref>.

## Constructors

### <a id="Jumbee_Console_KeyInputEvent__ctor_System_ConsoleKey_System_Char_Jumbee_Console_TerminalModifiers_"></a> KeyInputEvent\(ConsoleKey, char, TerminalModifiers\)

A key press.

```csharp
public KeyInputEvent(ConsoleKey Key, char KeyChar, TerminalModifiers Modifiers)
```

#### Parameters

`Key` ConsoleKey

`KeyChar` char

`Modifiers` [TerminalModifiers](Jumbee.Console.TerminalModifiers.md)

#### Remarks

Bridges to/from the existing <xref href="ConsoleGUI.Input.InputEvent" data-throw-if-not-resolved="false"></xref> path via <xref href="System.ConsoleKeyInfo" data-throw-if-not-resolved="false"></xref>.

## Properties

### <a id="Jumbee_Console_KeyInputEvent_Key"></a> Key

```csharp
public ConsoleKey Key { get; init; }
```

#### Property Value

 ConsoleKey

### <a id="Jumbee_Console_KeyInputEvent_KeyChar"></a> KeyChar

```csharp
public char KeyChar { get; init; }
```

#### Property Value

 char

### <a id="Jumbee_Console_KeyInputEvent_Modifiers"></a> Modifiers

```csharp
public TerminalModifiers Modifiers { get; init; }
```

#### Property Value

 [TerminalModifiers](Jumbee.Console.TerminalModifiers.md)

## Methods

### <a id="Jumbee_Console_KeyInputEvent_From_System_ConsoleKeyInfo_"></a> From\(ConsoleKeyInfo\)

Creates a <xref href="Jumbee.Console.KeyInputEvent" data-throw-if-not-resolved="false"></xref> from an existing <xref href="System.ConsoleKeyInfo" data-throw-if-not-resolved="false"></xref>.

```csharp
public static KeyInputEvent From(ConsoleKeyInfo key)
```

#### Parameters

`key` ConsoleKeyInfo

#### Returns

 [KeyInputEvent](Jumbee.Console.KeyInputEvent.md)

### <a id="Jumbee_Console_KeyInputEvent_ToConsoleKeyInfo"></a> ToConsoleKeyInfo\(\)

Converts this event to an equivalent <xref href="System.ConsoleKeyInfo" data-throw-if-not-resolved="false"></xref>.

```csharp
public ConsoleKeyInfo ToConsoleKeyInfo()
```

#### Returns

 ConsoleKeyInfo

