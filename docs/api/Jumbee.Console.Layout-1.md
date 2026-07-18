# <a id="Jumbee_Console_Layout_1"></a> Class Layout<T\>

Namespace: [Jumbee.Console](Jumbee.Console.md)  
Assembly: Jumbee.Console.dll  

Base class for Jumbee.Console layouts wrapping a ConsoleGUI layout control <code class="typeparamref">T</code> and exposing it through <xref href="Jumbee.Console.ILayout" data-throw-if-not-resolved="false"></xref>.

```csharp
public abstract class Layout<T> : ILayout, IFocusable where T : Control, IDrawingContextListener
```

#### Type Parameters

`T` 

#### Inheritance

object ← 
[Layout<T\>](Jumbee.Console.Layout\-1.md)

#### Implements

[ILayout](Jumbee.Console.ILayout.md), 
[IFocusable](Jumbee.Console.IFocusable.md)

## Constructors

### <a id="Jumbee_Console_Layout_1__ctor__0_"></a> Layout\(T\)

Initializes a new <xref href="Jumbee.Console.Layout%601" data-throw-if-not-resolved="false"></xref> wrapping the given ConsoleGUI <code class="paramref">control</code>.

```csharp
protected Layout(T control)
```

#### Parameters

`control` T

## Fields

### <a id="Jumbee_Console_Layout_1_control"></a> control

The wrapped ConsoleGUI layout control.

```csharp
public readonly T control
```

#### Field Value

 T

## Properties

### <a id="Jumbee_Console_Layout_1_CControl"></a> CControl

The underlying ConsoleGUI control this layout wraps.

```csharp
public IControl CControl { get; }
```

#### Property Value

 IControl

### <a id="Jumbee_Console_Layout_1_Columns"></a> Columns

The number of columns in the layout grid.

```csharp
public abstract int Columns { get; }
```

#### Property Value

 int

### <a id="Jumbee_Console_Layout_1_Context"></a> Context

The wrapped control's drawing context.

```csharp
public IDrawingContext Context { get; set; }
```

#### Property Value

 IDrawingContext

### <a id="Jumbee_Console_Layout_1_Controls"></a> Controls

The focusables in every grid cell, in row-major order.

```csharp
public IEnumerable<IFocusable> Controls { get; }
```

#### Property Value

 IEnumerable<[IFocusable](Jumbee.Console.IFocusable.md)\>

### <a id="Jumbee_Console_Layout_1_Focusable"></a> Focusable

When <a href="https://learn.microsoft.com/dotnet/csharp/language-reference/builtin-types/bool">true</a> (the default), this layout can hold focus.

```csharp
public bool Focusable { get; set; }
```

#### Property Value

 bool

### <a id="Jumbee_Console_Layout_1_FocusableControl"></a> FocusableControl

The focus target the UI registers for this layout — the layout itself.

```csharp
public IFocusable FocusableControl { get; }
```

#### Property Value

 [IFocusable](Jumbee.Console.IFocusable.md)

### <a id="Jumbee_Console_Layout_1_FocusedControl"></a> FocusedControl

The focused descendant within this layout (or <a href="https://learn.microsoft.com/dotnet/csharp/language-reference/keywords/null">null</a>), so a parent can tell that this layout
is on the focus path and route input into it.

```csharp
public virtual IFocusable? FocusedControl { get; }
```

#### Property Value

 [IFocusable](Jumbee.Console.IFocusable.md)?

#### Remarks

Walks <xref href="Jumbee.Console.Layout%601.Controls" data-throw-if-not-resolved="false"></xref> for the focused leaf; this is what lets keyboard input — and each ancestor
layout's tunnel (<xref href="Jumbee.Console.Layout%601.InterceptInput(Jumbee.Console.UI.InputEventArgs)" data-throw-if-not-resolved="false"></xref>) — reach a control even when the layout is nested several
levels deep.

### <a id="Jumbee_Console_Layout_1_HandlesInput"></a> HandlesInput

Always <a href="https://learn.microsoft.com/dotnet/csharp/language-reference/builtin-types/bool">true</a> — a layout routes input to its focused descendant.

```csharp
public bool HandlesInput { get; }
```

#### Property Value

 bool

### <a id="Jumbee_Console_Layout_1_IsFocused"></a> IsFocused

Whether this layout holds focus; setting it raises the focus events.

```csharp
public bool IsFocused { get; set; }
```

#### Property Value

 bool

### <a id="Jumbee_Console_Layout_1_Rows"></a> Rows

The number of rows in the layout grid.

```csharp
public abstract int Rows { get; }
```

#### Property Value

 int

### <a id="Jumbee_Console_Layout_1_Size"></a> Size

The wrapped control's laid-out size.

```csharp
public Size Size { get; }
```

#### Property Value

 Size

### <a id="Jumbee_Console_Layout_1_Item_System_Int32_System_Int32_"></a> this\[int, int\]

Gets the focusable at the given grid cell.

```csharp
public abstract IFocusable this[int row, int column] { get; }
```

#### Property Value

 [IFocusable](Jumbee.Console.IFocusable.md)

### <a id="Jumbee_Console_Layout_1_Item_ConsoleGUI_Space_Position_"></a> this\[Position\]

Gets the composited <xref href="ConsoleGUI.Data.Cell" data-throw-if-not-resolved="false"></xref> at <code class="paramref">position</code> from the wrapped control.

```csharp
public Cell this[Position position] { get; }
```

#### Property Value

 Cell

## Methods

### <a id="Jumbee_Console_Layout_1_InterceptInput_Jumbee_Console_UI_InputEventArgs_"></a> InterceptInput\(InputEventArgs\)

Lets a layout intercept input before it routes to the focused control. Return true if handled.

```csharp
protected virtual bool InterceptInput(UI.InputEventArgs inputEventArgs)
```

#### Parameters

`inputEventArgs` [UI](Jumbee.Console.UI.md).[InputEventArgs](Jumbee.Console.UI.InputEventArgs.md)

#### Returns

 bool

### <a id="Jumbee_Console_Layout_1_OnInput_Jumbee_Console_UI_InputEventArgs_"></a> OnInput\(InputEventArgs\)

Routes a UI input event: this layout's tunnel gets a first look, then the event is delivered to the focused descendant.

```csharp
public void OnInput(UI.InputEventArgs inputEventArgs)
```

#### Parameters

`inputEventArgs` [UI](Jumbee.Console.UI.md).[InputEventArgs](Jumbee.Console.UI.InputEventArgs.md)

### <a id="Jumbee_Console_Layout_1_OnPaste_System_String_"></a> OnPaste\(string\)

Forwards a bracketed-paste payload to each cell's focused descendant.

```csharp
public void OnPaste(string text)
```

#### Parameters

`text` string

### <a id="Jumbee_Console_Layout_1_OnRedraw_ConsoleGUI_Common_DrawingContext_"></a> OnRedraw\(DrawingContext\)

```csharp
public void OnRedraw(DrawingContext drawingContext)
```

#### Parameters

`drawingContext` DrawingContext

### <a id="Jumbee_Console_Layout_1_OnUpdate_ConsoleGUI_Common_DrawingContext_ConsoleGUI_Space_Rect_"></a> OnUpdate\(DrawingContext, Rect\)

```csharp
public void OnUpdate(DrawingContext drawingContext, Rect rect)
```

#### Parameters

`drawingContext` DrawingContext

`rect` Rect

### <a id="Jumbee_Console_Layout_1_OnFocus"></a> OnFocus

Raised when the layout gains focus.

```csharp
public event FocusableEventHandler? OnFocus
```

#### Event Type

 [FocusableEventHandler](Jumbee.Console.FocusableEventHandler.md)?

### <a id="Jumbee_Console_Layout_1_OnLostFocus"></a> OnLostFocus

Raised when the layout loses focus.

```csharp
public event FocusableEventHandler? OnLostFocus
```

#### Event Type

 [FocusableEventHandler](Jumbee.Console.FocusableEventHandler.md)?

