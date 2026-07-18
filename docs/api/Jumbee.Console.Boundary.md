# <a id="Jumbee_Console_Boundary"></a> Class Boundary

Namespace: [Jumbee.Console](Jumbee.Console.md)  
Assembly: Jumbee.Console.dll  

A single-child layout that pins its child's size.

```csharp
public class Boundary : Layout<Boundary>, ILayout, IFocusable
```

#### Inheritance

object ← 
[Layout<Boundary\>](Jumbee.Console.Layout\-1.md) ← 
[Boundary](Jumbee.Console.Boundary.md)

#### Implements

[ILayout](Jumbee.Console.ILayout.md), 
[IFocusable](Jumbee.Console.IFocusable.md)

#### Inherited Members

[Layout<Boundary\>.this\[int, int\]](Jumbee.Console.Layout\-1.md\#Jumbee\_Console\_Layout\_1\_Item\_System\_Int32\_System\_Int32\_), 
[Layout<Boundary\>.Rows](Jumbee.Console.Layout\-1.md\#Jumbee\_Console\_Layout\_1\_Rows), 
[Layout<Boundary\>.Columns](Jumbee.Console.Layout\-1.md\#Jumbee\_Console\_Layout\_1\_Columns), 
[Layout<Boundary\>.this\[Position\]](Jumbee.Console.Layout\-1.md\#Jumbee\_Console\_Layout\_1\_Item\_ConsoleGUI\_Space\_Position\_), 
[Layout<Boundary\>.Size](Jumbee.Console.Layout\-1.md\#Jumbee\_Console\_Layout\_1\_Size), 
[Layout<Boundary\>.CControl](Jumbee.Console.Layout\-1.md\#Jumbee\_Console\_Layout\_1\_CControl), 
[Layout<Boundary\>.Context](Jumbee.Console.Layout\-1.md\#Jumbee\_Console\_Layout\_1\_Context), 
[Layout<Boundary\>.Controls](Jumbee.Console.Layout\-1.md\#Jumbee\_Console\_Layout\_1\_Controls), 
[Layout<Boundary\>.Focusable](Jumbee.Console.Layout\-1.md\#Jumbee\_Console\_Layout\_1\_Focusable), 
[Layout<Boundary\>.FocusableControl](Jumbee.Console.Layout\-1.md\#Jumbee\_Console\_Layout\_1\_FocusableControl), 
[Layout<Boundary\>.IsFocused](Jumbee.Console.Layout\-1.md\#Jumbee\_Console\_Layout\_1\_IsFocused), 
[Layout<Boundary\>.HandlesInput](Jumbee.Console.Layout\-1.md\#Jumbee\_Console\_Layout\_1\_HandlesInput), 
[Layout<Boundary\>.FocusedControl](Jumbee.Console.Layout\-1.md\#Jumbee\_Console\_Layout\_1\_FocusedControl), 
[Layout<Boundary\>.OnFocus](Jumbee.Console.Layout\-1.md\#Jumbee\_Console\_Layout\_1\_OnFocus), 
[Layout<Boundary\>.OnLostFocus](Jumbee.Console.Layout\-1.md\#Jumbee\_Console\_Layout\_1\_OnLostFocus), 
[Layout<Boundary\>.OnRedraw\(DrawingContext\)](Jumbee.Console.Layout\-1.md\#Jumbee\_Console\_Layout\_1\_OnRedraw\_ConsoleGUI\_Common\_DrawingContext\_), 
[Layout<Boundary\>.OnUpdate\(DrawingContext, Rect\)](Jumbee.Console.Layout\-1.md\#Jumbee\_Console\_Layout\_1\_OnUpdate\_ConsoleGUI\_Common\_DrawingContext\_ConsoleGUI\_Space\_Rect\_), 
[Layout<Boundary\>.OnInput\(UI.InputEventArgs\)](Jumbee.Console.Layout\-1.md\#Jumbee\_Console\_Layout\_1\_OnInput\_Jumbee\_Console\_UI\_InputEventArgs\_), 
[Layout<Boundary\>.InterceptInput\(UI.InputEventArgs\)](Jumbee.Console.Layout\-1.md\#Jumbee\_Console\_Layout\_1\_InterceptInput\_Jumbee\_Console\_UI\_InputEventArgs\_), 
[Layout<Boundary\>.OnPaste\(string\)](Jumbee.Console.Layout\-1.md\#Jumbee\_Console\_Layout\_1\_OnPaste\_System\_String\_), 
[Layout<Boundary\>.control](Jumbee.Console.Layout\-1.md\#Jumbee\_Console\_Layout\_1\_control)

## Remarks

Wrap a control or layout that would otherwise fill its slot to give it a fixed (or bounded) extent — e.g. cap a
toolbar's height to one row so it can be docked at the top of a <xref href="Jumbee.Console.DockPanel" data-throw-if-not-resolved="false"></xref> without collapsing the fill
region (a <xref href="Jumbee.Console.HorizontalStackPanel" data-throw-if-not-resolved="false"></xref> on its own expands to the full height). Leave a dimension unset to let
the child size freely within the slot.

## Constructors

### <a id="Jumbee_Console_Boundary__ctor_Jumbee_Console_IFocusable_System_Nullable_System_Int32__System_Nullable_System_Int32__"></a> Boundary\(IFocusable, int?, int?\)

```csharp
public Boundary(IFocusable content, int? width = null, int? height = null)
```

#### Parameters

`content` [IFocusable](Jumbee.Console.IFocusable.md)

The child to bound.

`width` int?

Fixed width in cells, or <a href="https://learn.microsoft.com/dotnet/csharp/language-reference/keywords/null">null</a> to size freely.

`height` int?

Fixed height in cells, or <a href="https://learn.microsoft.com/dotnet/csharp/language-reference/keywords/null">null</a> to size freely.

## Properties

### <a id="Jumbee_Console_Boundary_Columns"></a> Columns

Number of columns in the layout grid (always 1).

```csharp
public override int Columns { get; }
```

#### Property Value

 int

### <a id="Jumbee_Console_Boundary_Content"></a> Content

The bounded child.

```csharp
public IFocusable Content { get; }
```

#### Property Value

 [IFocusable](Jumbee.Console.IFocusable.md)

### <a id="Jumbee_Console_Boundary_Height"></a> Height

Fixed height in cells (sets min = max = value), or <a href="https://learn.microsoft.com/dotnet/csharp/language-reference/keywords/null">null</a> to size freely.

```csharp
public int? Height { set; }
```

#### Property Value

 int?

### <a id="Jumbee_Console_Boundary_MaxHeight"></a> MaxHeight

Maximum height in cells, or <a href="https://learn.microsoft.com/dotnet/csharp/language-reference/keywords/null">null</a> for none.

```csharp
public int? MaxHeight { get; set; }
```

#### Property Value

 int?

### <a id="Jumbee_Console_Boundary_MaxWidth"></a> MaxWidth

Maximum width in cells, or <a href="https://learn.microsoft.com/dotnet/csharp/language-reference/keywords/null">null</a> for none.

```csharp
public int? MaxWidth { get; set; }
```

#### Property Value

 int?

### <a id="Jumbee_Console_Boundary_MinHeight"></a> MinHeight

Minimum height in cells, or <a href="https://learn.microsoft.com/dotnet/csharp/language-reference/keywords/null">null</a> for none.

```csharp
public int? MinHeight { get; set; }
```

#### Property Value

 int?

### <a id="Jumbee_Console_Boundary_MinWidth"></a> MinWidth

Minimum width in cells, or <a href="https://learn.microsoft.com/dotnet/csharp/language-reference/keywords/null">null</a> for none.

```csharp
public int? MinWidth { get; set; }
```

#### Property Value

 int?

### <a id="Jumbee_Console_Boundary_Rows"></a> Rows

Number of rows in the layout grid (always 1).

```csharp
public override int Rows { get; }
```

#### Property Value

 int

### <a id="Jumbee_Console_Boundary_Width"></a> Width

Fixed width in cells (sets min = max = value), or <a href="https://learn.microsoft.com/dotnet/csharp/language-reference/keywords/null">null</a> to size freely.

```csharp
public int? Width { set; }
```

#### Property Value

 int?

### <a id="Jumbee_Console_Boundary_Item_System_Int32_System_Int32_"></a> this\[int, int\]

Gets the bounded child at cell (0, 0).

```csharp
public override IFocusable this[int row, int column] { get; }
```

#### Property Value

 [IFocusable](Jumbee.Console.IFocusable.md)

