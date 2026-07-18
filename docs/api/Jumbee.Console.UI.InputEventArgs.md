# <a id="Jumbee_Console_UI_InputEventArgs"></a> Class UI.InputEventArgs

Namespace: [Jumbee.Console](Jumbee.Console.md)  
Assembly: Jumbee.Console.dll  

Arguments for control input handling, wrapping the decoded <xref href="Jumbee.Console.UI.InputEventArgs.InputEvent" data-throw-if-not-resolved="false"></xref>.

```csharp
public class UI.InputEventArgs : EventArgs
```

#### Inheritance

object ← 
EventArgs ← 
[UI.InputEventArgs](Jumbee.Console.UI.InputEventArgs.md)

## Constructors

### <a id="Jumbee_Console_UI_InputEventArgs__ctor"></a> InputEventArgs\(\)

Initializes an empty <xref href="Jumbee.Console.UI.InputEventArgs" data-throw-if-not-resolved="false"></xref>.

```csharp
public InputEventArgs()
```

### <a id="Jumbee_Console_UI_InputEventArgs__ctor_ConsoleGUI_Input_InputEvent_"></a> InputEventArgs\(InputEvent?\)

Initializes a new <xref href="Jumbee.Console.UI.InputEventArgs" data-throw-if-not-resolved="false"></xref> carrying <code class="paramref">inputEvent</code>.

```csharp
public InputEventArgs(InputEvent? inputEvent)
```

#### Parameters

`inputEvent` InputEvent?

## Properties

### <a id="Jumbee_Console_UI_InputEventArgs_InputEvent"></a> InputEvent

The decoded input event being dispatched, or <a href="https://learn.microsoft.com/dotnet/csharp/language-reference/keywords/null">null</a>.

```csharp
public InputEvent? InputEvent { get; }
```

#### Property Value

 InputEvent?

