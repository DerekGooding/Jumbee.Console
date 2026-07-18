# <a id="Jumbee_Console_ResizeInputEvent"></a> Class ResizeInputEvent

Namespace: [Jumbee.Console](Jumbee.Console.md)  
Assembly: Jumbee.Console.dll  

Terminal resized. Not decoded from the char stream — raised by the input source on a resize signal.

```csharp
public sealed record ResizeInputEvent : TerminalInputEvent
```

#### Inheritance

object ← 
[TerminalInputEvent](Jumbee.Console.TerminalInputEvent.md) ← 
[ResizeInputEvent](Jumbee.Console.ResizeInputEvent.md)

## Constructors

### <a id="Jumbee_Console_ResizeInputEvent__ctor_System_Int32_System_Int32_"></a> ResizeInputEvent\(int, int\)

Terminal resized. Not decoded from the char stream — raised by the input source on a resize signal.

```csharp
public ResizeInputEvent(int Width, int Height)
```

#### Parameters

`Width` int

`Height` int

## Properties

### <a id="Jumbee_Console_ResizeInputEvent_Height"></a> Height

```csharp
public int Height { get; init; }
```

#### Property Value

 int

### <a id="Jumbee_Console_ResizeInputEvent_Width"></a> Width

```csharp
public int Width { get; init; }
```

#### Property Value

 int

