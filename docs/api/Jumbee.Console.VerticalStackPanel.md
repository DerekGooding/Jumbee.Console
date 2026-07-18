# <a id="Jumbee_Console_VerticalStackPanel"></a> Class VerticalStackPanel

Namespace: [Jumbee.Console](Jumbee.Console.md)  
Assembly: Jumbee.Console.dll  

A layout that arranges its child controls in a single vertical column.

```csharp
public class VerticalStackPanel : Layout<VerticalStackPanel>, ILayout, IFocusable
```

#### Inheritance

object ← 
[Layout<VerticalStackPanel\>](Jumbee.Console.Layout\-1.md) ← 
[VerticalStackPanel](Jumbee.Console.VerticalStackPanel.md)

#### Implements

[ILayout](Jumbee.Console.ILayout.md), 
[IFocusable](Jumbee.Console.IFocusable.md)

#### Inherited Members

[Layout<VerticalStackPanel\>.this\[int, int\]](Jumbee.Console.Layout\-1.md\#Jumbee\_Console\_Layout\_1\_Item\_System\_Int32\_System\_Int32\_), 
[Layout<VerticalStackPanel\>.Rows](Jumbee.Console.Layout\-1.md\#Jumbee\_Console\_Layout\_1\_Rows), 
[Layout<VerticalStackPanel\>.Columns](Jumbee.Console.Layout\-1.md\#Jumbee\_Console\_Layout\_1\_Columns), 
[Layout<VerticalStackPanel\>.this\[Position\]](Jumbee.Console.Layout\-1.md\#Jumbee\_Console\_Layout\_1\_Item\_ConsoleGUI\_Space\_Position\_), 
[Layout<VerticalStackPanel\>.Size](Jumbee.Console.Layout\-1.md\#Jumbee\_Console\_Layout\_1\_Size), 
[Layout<VerticalStackPanel\>.CControl](Jumbee.Console.Layout\-1.md\#Jumbee\_Console\_Layout\_1\_CControl), 
[Layout<VerticalStackPanel\>.Context](Jumbee.Console.Layout\-1.md\#Jumbee\_Console\_Layout\_1\_Context), 
[Layout<VerticalStackPanel\>.Controls](Jumbee.Console.Layout\-1.md\#Jumbee\_Console\_Layout\_1\_Controls), 
[Layout<VerticalStackPanel\>.Focusable](Jumbee.Console.Layout\-1.md\#Jumbee\_Console\_Layout\_1\_Focusable), 
[Layout<VerticalStackPanel\>.FocusableControl](Jumbee.Console.Layout\-1.md\#Jumbee\_Console\_Layout\_1\_FocusableControl), 
[Layout<VerticalStackPanel\>.IsFocused](Jumbee.Console.Layout\-1.md\#Jumbee\_Console\_Layout\_1\_IsFocused), 
[Layout<VerticalStackPanel\>.HandlesInput](Jumbee.Console.Layout\-1.md\#Jumbee\_Console\_Layout\_1\_HandlesInput), 
[Layout<VerticalStackPanel\>.FocusedControl](Jumbee.Console.Layout\-1.md\#Jumbee\_Console\_Layout\_1\_FocusedControl), 
[Layout<VerticalStackPanel\>.OnFocus](Jumbee.Console.Layout\-1.md\#Jumbee\_Console\_Layout\_1\_OnFocus), 
[Layout<VerticalStackPanel\>.OnLostFocus](Jumbee.Console.Layout\-1.md\#Jumbee\_Console\_Layout\_1\_OnLostFocus), 
[Layout<VerticalStackPanel\>.OnRedraw\(DrawingContext\)](Jumbee.Console.Layout\-1.md\#Jumbee\_Console\_Layout\_1\_OnRedraw\_ConsoleGUI\_Common\_DrawingContext\_), 
[Layout<VerticalStackPanel\>.OnUpdate\(DrawingContext, Rect\)](Jumbee.Console.Layout\-1.md\#Jumbee\_Console\_Layout\_1\_OnUpdate\_ConsoleGUI\_Common\_DrawingContext\_ConsoleGUI\_Space\_Rect\_), 
[Layout<VerticalStackPanel\>.OnInput\(UI.InputEventArgs\)](Jumbee.Console.Layout\-1.md\#Jumbee\_Console\_Layout\_1\_OnInput\_Jumbee\_Console\_UI\_InputEventArgs\_), 
[Layout<VerticalStackPanel\>.InterceptInput\(UI.InputEventArgs\)](Jumbee.Console.Layout\-1.md\#Jumbee\_Console\_Layout\_1\_InterceptInput\_Jumbee\_Console\_UI\_InputEventArgs\_), 
[Layout<VerticalStackPanel\>.OnPaste\(string\)](Jumbee.Console.Layout\-1.md\#Jumbee\_Console\_Layout\_1\_OnPaste\_System\_String\_), 
[Layout<VerticalStackPanel\>.control](Jumbee.Console.Layout\-1.md\#Jumbee\_Console\_Layout\_1\_control)

## Constructors

### <a id="Jumbee_Console_VerticalStackPanel__ctor_Jumbee_Console_IFocusable___"></a> VerticalStackPanel\(params IFocusable\[\]?\)

Initializes a new <xref href="Jumbee.Console.VerticalStackPanel" data-throw-if-not-resolved="false"></xref> containing the given <code class="paramref">controls</code>.

```csharp
public VerticalStackPanel(params IFocusable[]? controls)
```

#### Parameters

`controls` [IFocusable](Jumbee.Console.IFocusable.md)\[\]?

## Properties

### <a id="Jumbee_Console_VerticalStackPanel_Columns"></a> Columns

Number of columns in the layout grid (always 1).

```csharp
public override int Columns { get; }
```

#### Property Value

 int

### <a id="Jumbee_Console_VerticalStackPanel_Rows"></a> Rows

Number of rows, i.e. the child count.

```csharp
public override int Rows { get; }
```

#### Property Value

 int

### <a id="Jumbee_Console_VerticalStackPanel_Item_System_Int32_System_Int32_"></a> this\[int, int\]

Gets the control at the given <code class="paramref">row</code> (<code class="paramref">column</code> must be 0).

```csharp
public override IFocusable this[int row, int column] { get; }
```

#### Property Value

 [IFocusable](Jumbee.Console.IFocusable.md)

## Methods

### <a id="Jumbee_Console_VerticalStackPanel_Add_Jumbee_Console_IFocusable___"></a> Add\(params IFocusable\[\]\)

Appends the given <code class="paramref">controls</code> to the column.

```csharp
public void Add(params IFocusable[] controls)
```

#### Parameters

`controls` [IFocusable](Jumbee.Console.IFocusable.md)\[\]

### <a id="Jumbee_Console_VerticalStackPanel_Remove_Jumbee_Console_IFocusable___"></a> Remove\(params IFocusable\[\]\)

Removes the given <code class="paramref">controls</code> from the column.

```csharp
public void Remove(params IFocusable[] controls)
```

#### Parameters

`controls` [IFocusable](Jumbee.Console.IFocusable.md)\[\]

