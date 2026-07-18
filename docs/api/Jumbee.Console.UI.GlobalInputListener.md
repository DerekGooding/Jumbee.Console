# <a id="Jumbee_Console_UI_GlobalInputListener"></a> Class UI.GlobalInputListener

Namespace: [Jumbee.Console](Jumbee.Console.md)  
Assembly: Jumbee.Console.dll  

Input listener that dispatches globally-registered hotkeys before any control sees the event.

```csharp
public class UI.GlobalInputListener
```

#### Inheritance

object ← 
[UI.GlobalInputListener](Jumbee.Console.UI.GlobalInputListener.md)

## Methods

### <a id="Jumbee_Console_UI_GlobalInputListener_OnInput_ConsoleGUI_Input_InputEvent_"></a> OnInput\(InputEvent\)

Invokes the registered action for a matching global hotkey and marks the event handled.

```csharp
public void OnInput(InputEvent inputEvent)
```

#### Parameters

`inputEvent` InputEvent

