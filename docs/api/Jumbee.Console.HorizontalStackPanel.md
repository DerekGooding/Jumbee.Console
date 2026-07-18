# <a id="Jumbee_Console_HorizontalStackPanel"></a> Class HorizontalStackPanel

Namespace: [Jumbee.Console](Jumbee.Console.md)  
Assembly: Jumbee.Console.dll  

A layout that arranges its child controls in a single horizontal row.

```csharp
public class HorizontalStackPanel : Layout<HorizontalStackPanel>, ILayout, IFocusable
```

#### Inheritance

object ← 
[Layout<HorizontalStackPanel\>](Jumbee.Console.Layout\-1.md) ← 
[HorizontalStackPanel](Jumbee.Console.HorizontalStackPanel.md)

#### Implements

[ILayout](Jumbee.Console.ILayout.md), 
[IFocusable](Jumbee.Console.IFocusable.md)

#### Inherited Members

[Layout<HorizontalStackPanel\>.this\[int, int\]](Jumbee.Console.Layout\-1.md\#Jumbee\_Console\_Layout\_1\_Item\_System\_Int32\_System\_Int32\_), 
[Layout<HorizontalStackPanel\>.Rows](Jumbee.Console.Layout\-1.md\#Jumbee\_Console\_Layout\_1\_Rows), 
[Layout<HorizontalStackPanel\>.Columns](Jumbee.Console.Layout\-1.md\#Jumbee\_Console\_Layout\_1\_Columns), 
[Layout<HorizontalStackPanel\>.this\[Position\]](Jumbee.Console.Layout\-1.md\#Jumbee\_Console\_Layout\_1\_Item\_ConsoleGUI\_Space\_Position\_), 
[Layout<HorizontalStackPanel\>.Size](Jumbee.Console.Layout\-1.md\#Jumbee\_Console\_Layout\_1\_Size), 
[Layout<HorizontalStackPanel\>.CControl](Jumbee.Console.Layout\-1.md\#Jumbee\_Console\_Layout\_1\_CControl), 
[Layout<HorizontalStackPanel\>.Context](Jumbee.Console.Layout\-1.md\#Jumbee\_Console\_Layout\_1\_Context), 
[Layout<HorizontalStackPanel\>.Controls](Jumbee.Console.Layout\-1.md\#Jumbee\_Console\_Layout\_1\_Controls), 
[Layout<HorizontalStackPanel\>.Focusable](Jumbee.Console.Layout\-1.md\#Jumbee\_Console\_Layout\_1\_Focusable), 
[Layout<HorizontalStackPanel\>.FocusableControl](Jumbee.Console.Layout\-1.md\#Jumbee\_Console\_Layout\_1\_FocusableControl), 
[Layout<HorizontalStackPanel\>.IsFocused](Jumbee.Console.Layout\-1.md\#Jumbee\_Console\_Layout\_1\_IsFocused), 
[Layout<HorizontalStackPanel\>.HandlesInput](Jumbee.Console.Layout\-1.md\#Jumbee\_Console\_Layout\_1\_HandlesInput), 
[Layout<HorizontalStackPanel\>.FocusedControl](Jumbee.Console.Layout\-1.md\#Jumbee\_Console\_Layout\_1\_FocusedControl), 
[Layout<HorizontalStackPanel\>.OnFocus](Jumbee.Console.Layout\-1.md\#Jumbee\_Console\_Layout\_1\_OnFocus), 
[Layout<HorizontalStackPanel\>.OnLostFocus](Jumbee.Console.Layout\-1.md\#Jumbee\_Console\_Layout\_1\_OnLostFocus), 
[Layout<HorizontalStackPanel\>.OnRedraw\(DrawingContext\)](Jumbee.Console.Layout\-1.md\#Jumbee\_Console\_Layout\_1\_OnRedraw\_ConsoleGUI\_Common\_DrawingContext\_), 
[Layout<HorizontalStackPanel\>.OnUpdate\(DrawingContext, Rect\)](Jumbee.Console.Layout\-1.md\#Jumbee\_Console\_Layout\_1\_OnUpdate\_ConsoleGUI\_Common\_DrawingContext\_ConsoleGUI\_Space\_Rect\_), 
[Layout<HorizontalStackPanel\>.OnInput\(UI.InputEventArgs\)](Jumbee.Console.Layout\-1.md\#Jumbee\_Console\_Layout\_1\_OnInput\_Jumbee\_Console\_UI\_InputEventArgs\_), 
[Layout<HorizontalStackPanel\>.InterceptInput\(UI.InputEventArgs\)](Jumbee.Console.Layout\-1.md\#Jumbee\_Console\_Layout\_1\_InterceptInput\_Jumbee\_Console\_UI\_InputEventArgs\_), 
[Layout<HorizontalStackPanel\>.OnPaste\(string\)](Jumbee.Console.Layout\-1.md\#Jumbee\_Console\_Layout\_1\_OnPaste\_System\_String\_), 
[Layout<HorizontalStackPanel\>.control](Jumbee.Console.Layout\-1.md\#Jumbee\_Console\_Layout\_1\_control)

## Constructors

### <a id="Jumbee_Console_HorizontalStackPanel__ctor_Jumbee_Console_IFocusable___"></a> HorizontalStackPanel\(params IFocusable\[\]?\)

Initializes a new <xref href="Jumbee.Console.HorizontalStackPanel" data-throw-if-not-resolved="false"></xref> containing the given <code class="paramref">controls</code>.

```csharp
public HorizontalStackPanel(params IFocusable[]? controls)
```

#### Parameters

`controls` [IFocusable](Jumbee.Console.IFocusable.md)\[\]?

## Properties

### <a id="Jumbee_Console_HorizontalStackPanel_Columns"></a> Columns

Number of columns, i.e. the child count.

```csharp
public override int Columns { get; }
```

#### Property Value

 int

### <a id="Jumbee_Console_HorizontalStackPanel_Rows"></a> Rows

Number of rows in the layout grid (always 1).

```csharp
public override int Rows { get; }
```

#### Property Value

 int

### <a id="Jumbee_Console_HorizontalStackPanel_Item_System_Int32_System_Int32_"></a> this\[int, int\]

Gets the control at the given <code class="paramref">column</code> (<code class="paramref">row</code> must be 0).

```csharp
public override IFocusable this[int row, int column] { get; }
```

#### Property Value

 [IFocusable](Jumbee.Console.IFocusable.md)

## Methods

### <a id="Jumbee_Console_HorizontalStackPanel_Add_Jumbee_Console_IFocusable___"></a> Add\(params IFocusable\[\]\)

Appends the given <code class="paramref">controls</code> to the row.

```csharp
public void Add(params IFocusable[] controls)
```

#### Parameters

`controls` [IFocusable](Jumbee.Console.IFocusable.md)\[\]

### <a id="Jumbee_Console_HorizontalStackPanel_Remove_Jumbee_Console_IFocusable___"></a> Remove\(params IFocusable\[\]\)

Removes the given <code class="paramref">controls</code> from the row.

```csharp
public void Remove(params IFocusable[] controls)
```

#### Parameters

`controls` [IFocusable](Jumbee.Console.IFocusable.md)\[\]

