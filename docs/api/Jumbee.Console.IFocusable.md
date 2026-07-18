# <a id="Jumbee_Console_IFocusable"></a> Interface IFocusable

Namespace: [Jumbee.Console](Jumbee.Console.md)  
Assembly: Jumbee.Console.dll  

A control that can receive keyboard focus and input.

```csharp
public interface IFocusable
```

## Properties

### <a id="Jumbee_Console_IFocusable_Focusable"></a> Focusable

Whether the control can receive focus.

```csharp
bool Focusable { get; set; }
```

#### Property Value

 bool

### <a id="Jumbee_Console_IFocusable_FocusableControl"></a> FocusableControl

The control that actually holds focus (this control, or a focusable descendant/wrapped control).

```csharp
IFocusable FocusableControl { get; }
```

#### Property Value

 [IFocusable](Jumbee.Console.IFocusable.md)

### <a id="Jumbee_Console_IFocusable_FocusedControl"></a> FocusedControl

The focusable control if this one is focusable and currently focused, otherwise <a href="https://learn.microsoft.com/dotnet/csharp/language-reference/keywords/null">null</a>.

```csharp
IFocusable? FocusedControl { get; }
```

#### Property Value

 [IFocusable](Jumbee.Console.IFocusable.md)?

### <a id="Jumbee_Console_IFocusable_HandlesInput"></a> HandlesInput

Whether the control consumes keyboard input while focused.

```csharp
bool HandlesInput { get; }
```

#### Property Value

 bool

### <a id="Jumbee_Console_IFocusable_IsFocused"></a> IsFocused

Whether the control currently has focus.

```csharp
bool IsFocused { get; set; }
```

#### Property Value

 bool

## Methods

### <a id="Jumbee_Console_IFocusable_Focus"></a> Focus\(\)

Gives the control focus.

```csharp
void Focus()
```

### <a id="Jumbee_Console_IFocusable_OnInput_Jumbee_Console_UI_InputEventArgs_"></a> OnInput\(InputEventArgs\)

Handles an input event routed to this control.

```csharp
void OnInput(UI.InputEventArgs inputEventArgs)
```

#### Parameters

`inputEventArgs` [UI](Jumbee.Console.UI.md).[InputEventArgs](Jumbee.Console.UI.InputEventArgs.md)

### <a id="Jumbee_Console_IFocusable_OnPaste_System_String_"></a> OnPaste\(string\)

Delivers a bracketed-paste payload as a single unit. Default no-op; overridden by text controls.

```csharp
void OnPaste(string text)
```

#### Parameters

`text` string

### <a id="Jumbee_Console_IFocusable_UnFocus"></a> UnFocus\(\)

Removes focus from the control.

```csharp
void UnFocus()
```

### <a id="Jumbee_Console_IFocusable_OnFocus"></a> OnFocus

Raised when the control gains focus.

```csharp
event FocusableEventHandler OnFocus
```

#### Event Type

 [FocusableEventHandler](Jumbee.Console.FocusableEventHandler.md)

### <a id="Jumbee_Console_IFocusable_OnLostFocus"></a> OnLostFocus

Raised when the control loses focus.

```csharp
event FocusableEventHandler OnLostFocus
```

#### Event Type

 [FocusableEventHandler](Jumbee.Console.FocusableEventHandler.md)

