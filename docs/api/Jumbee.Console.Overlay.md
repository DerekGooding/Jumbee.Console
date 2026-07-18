# <a id="Jumbee_Console_Overlay"></a> Class Overlay

Namespace: [Jumbee.Console](Jumbee.Console.md)  
Assembly: Jumbee.Console.dll  

A layered layout: a persistent <xref href="Jumbee.Console.Overlay.Bottom" data-throw-if-not-resolved="false"></xref> layer with an optional floating popup composited on top
(wraps ConsoleGUI's <xref href="ConsoleGUI.Controls.Overlay" data-throw-if-not-resolved="false"></xref>).

```csharp
public class Overlay : Layout<Overlay>, ILayout, IFocusable
```

#### Inheritance

object ← 
[Layout<Overlay\>](Jumbee.Console.Layout\-1.md) ← 
[Overlay](Jumbee.Console.Overlay.md)

#### Implements

[ILayout](Jumbee.Console.ILayout.md), 
[IFocusable](Jumbee.Console.IFocusable.md)

#### Inherited Members

[Layout<Overlay\>.this\[int, int\]](Jumbee.Console.Layout\-1.md\#Jumbee\_Console\_Layout\_1\_Item\_System\_Int32\_System\_Int32\_), 
[Layout<Overlay\>.Rows](Jumbee.Console.Layout\-1.md\#Jumbee\_Console\_Layout\_1\_Rows), 
[Layout<Overlay\>.Columns](Jumbee.Console.Layout\-1.md\#Jumbee\_Console\_Layout\_1\_Columns), 
[Layout<Overlay\>.this\[Position\]](Jumbee.Console.Layout\-1.md\#Jumbee\_Console\_Layout\_1\_Item\_ConsoleGUI\_Space\_Position\_), 
[Layout<Overlay\>.Size](Jumbee.Console.Layout\-1.md\#Jumbee\_Console\_Layout\_1\_Size), 
[Layout<Overlay\>.CControl](Jumbee.Console.Layout\-1.md\#Jumbee\_Console\_Layout\_1\_CControl), 
[Layout<Overlay\>.Context](Jumbee.Console.Layout\-1.md\#Jumbee\_Console\_Layout\_1\_Context), 
[Layout<Overlay\>.Controls](Jumbee.Console.Layout\-1.md\#Jumbee\_Console\_Layout\_1\_Controls), 
[Layout<Overlay\>.Focusable](Jumbee.Console.Layout\-1.md\#Jumbee\_Console\_Layout\_1\_Focusable), 
[Layout<Overlay\>.FocusableControl](Jumbee.Console.Layout\-1.md\#Jumbee\_Console\_Layout\_1\_FocusableControl), 
[Layout<Overlay\>.IsFocused](Jumbee.Console.Layout\-1.md\#Jumbee\_Console\_Layout\_1\_IsFocused), 
[Layout<Overlay\>.HandlesInput](Jumbee.Console.Layout\-1.md\#Jumbee\_Console\_Layout\_1\_HandlesInput), 
[Layout<Overlay\>.FocusedControl](Jumbee.Console.Layout\-1.md\#Jumbee\_Console\_Layout\_1\_FocusedControl), 
[Layout<Overlay\>.OnFocus](Jumbee.Console.Layout\-1.md\#Jumbee\_Console\_Layout\_1\_OnFocus), 
[Layout<Overlay\>.OnLostFocus](Jumbee.Console.Layout\-1.md\#Jumbee\_Console\_Layout\_1\_OnLostFocus), 
[Layout<Overlay\>.OnRedraw\(DrawingContext\)](Jumbee.Console.Layout\-1.md\#Jumbee\_Console\_Layout\_1\_OnRedraw\_ConsoleGUI\_Common\_DrawingContext\_), 
[Layout<Overlay\>.OnUpdate\(DrawingContext, Rect\)](Jumbee.Console.Layout\-1.md\#Jumbee\_Console\_Layout\_1\_OnUpdate\_ConsoleGUI\_Common\_DrawingContext\_ConsoleGUI\_Space\_Rect\_), 
[Layout<Overlay\>.OnInput\(UI.InputEventArgs\)](Jumbee.Console.Layout\-1.md\#Jumbee\_Console\_Layout\_1\_OnInput\_Jumbee\_Console\_UI\_InputEventArgs\_), 
[Layout<Overlay\>.InterceptInput\(UI.InputEventArgs\)](Jumbee.Console.Layout\-1.md\#Jumbee\_Console\_Layout\_1\_InterceptInput\_Jumbee\_Console\_UI\_InputEventArgs\_), 
[Layout<Overlay\>.OnPaste\(string\)](Jumbee.Console.Layout\-1.md\#Jumbee\_Console\_Layout\_1\_OnPaste\_System\_String\_), 
[Layout<Overlay\>.control](Jumbee.Console.Layout\-1.md\#Jumbee\_Console\_Layout\_1\_control)

## Remarks

Where the popup has no content the bottom shows through, so a small centered/anchored popup floats over the main
UI. Use <xref href="Jumbee.Console.Overlay.Show(Jumbee.Console.Control)" data-throw-if-not-resolved="false"></xref> / <xref href="Jumbee.Console.Overlay.Hide" data-throw-if-not-resolved="false"></xref> for dropdowns, dialogs, tooltips, etc. While shown,
keyboard input goes to the focused popup; clicking the popup works as normal, and (by default) clicking outside it
closes it.

## Constructors

### <a id="Jumbee_Console_Overlay__ctor_Jumbee_Console_ILayout_"></a> Overlay\(ILayout\)

Initializes a new <xref href="Jumbee.Console.Overlay" data-throw-if-not-resolved="false"></xref> with <code class="paramref">bottom</code> as its persistent base layer.

```csharp
public Overlay(ILayout bottom)
```

#### Parameters

`bottom` [ILayout](Jumbee.Console.ILayout.md)

## Properties

### <a id="Jumbee_Console_Overlay_Bottom"></a> Bottom

The persistent base layer.

```csharp
public ILayout Bottom { get; }
```

#### Property Value

 [ILayout](Jumbee.Console.ILayout.md)

### <a id="Jumbee_Console_Overlay_CloseKey"></a> CloseKey

Key that closes any open popup, intercepted before the popup sees it. <a href="https://learn.microsoft.com/dotnet/csharp/language-reference/keywords/null">null</a> disables it.

```csharp
public ConsoleKey? CloseKey { get; set; }
```

#### Property Value

 ConsoleKey?

### <a id="Jumbee_Console_Overlay_CloseOnFocusLost"></a> CloseOnFocusLost

When <a href="https://learn.microsoft.com/dotnet/csharp/language-reference/builtin-types/bool">true</a> (default), a non-modal popup closes when it loses focus (e.g. a click outside).

```csharp
public bool CloseOnFocusLost { get; set; }
```

#### Property Value

 bool

### <a id="Jumbee_Console_Overlay_Columns"></a> Columns

Number of columns in the layout grid (1 while a capturing popup is shown, otherwise the bottom layer's columns).

```csharp
public override int Columns { get; }
```

#### Property Value

 int

### <a id="Jumbee_Console_Overlay_IsModal"></a> IsModal

<a href="https://learn.microsoft.com/dotnet/csharp/language-reference/builtin-types/bool">true</a> when the current popup is modal (shown over a click-blocking scrim).

```csharp
public bool IsModal { get; }
```

#### Property Value

 bool

### <a id="Jumbee_Console_Overlay_IsShowing"></a> IsShowing

<a href="https://learn.microsoft.com/dotnet/csharp/language-reference/builtin-types/bool">true</a> when a popup is currently shown.

```csharp
public bool IsShowing { get; }
```

#### Property Value

 bool

### <a id="Jumbee_Console_Overlay_ModalDim"></a> ModalDim

How strongly a modal scrim dims the layer beneath it: 0 = fully see-through, 1 = a solid
    <xref href="Jumbee.Console.Overlay.ModalScrim" data-throw-if-not-resolved="false"></xref> fill (the classic opaque modal).

```csharp
public float ModalDim { get; set; }
```

#### Property Value

 float

#### Remarks

Defaults to the theme's <xref href="Jumbee.Console.IStyleTheme.ScrimDim" data-throw-if-not-resolved="false"></xref> (0.6), so the controls behind show through,
    dimmed, while the popup stands out; set it to override per overlay. The scrim blocks clicks regardless of this
    value.

### <a id="Jumbee_Console_Overlay_ModalScrim"></a> ModalScrim

The tint a modal scrim blends the layer beneath toward (see <xref href="Jumbee.Console.Overlay.ModalDim" data-throw-if-not-resolved="false"></xref>).

```csharp
public Color ModalScrim { get; set; }
```

#### Property Value

 [Color](Jumbee.Console.Color.md)

#### Remarks

Defaults to the theme's <xref href="Jumbee.Console.IStyleTheme.Scrim" data-throw-if-not-resolved="false"></xref> colour (picked up live on a theme switch);
    set it to override per overlay.

### <a id="Jumbee_Console_Overlay_Rows"></a> Rows

Number of rows in the layout grid (1 while a capturing popup is shown, otherwise the bottom layer's rows).

```csharp
public override int Rows { get; }
```

#### Property Value

 int

### <a id="Jumbee_Console_Overlay_Top"></a> Top

The popup currently shown, or <a href="https://learn.microsoft.com/dotnet/csharp/language-reference/keywords/null">null</a>.

```csharp
public Control? Top { get; }
```

#### Property Value

 [Control](Jumbee.Console.Control.md)?

### <a id="Jumbee_Console_Overlay_Item_System_Int32_System_Int32_"></a> this\[int, int\]

Gets the control at the given <code class="paramref">row</code> and <code class="paramref">column</code> (the popup while one is shown, otherwise the bottom layer's cell).

```csharp
public override IFocusable this[int row, int column] { get; }
```

#### Property Value

 [IFocusable](Jumbee.Console.IFocusable.md)

## Methods

### <a id="Jumbee_Console_Overlay_Hide"></a> Hide\(\)

Close the popup and restore focus to whatever was focused before it was shown.

```csharp
public void Hide()
```

### <a id="Jumbee_Console_Overlay_InterceptInput_Jumbee_Console_UI_InputEventArgs_"></a> InterceptInput\(InputEventArgs\)

Closes any open popup when <xref href="Jumbee.Console.Overlay.CloseKey" data-throw-if-not-resolved="false"></xref> is pressed, before the popup sees the key.

```csharp
protected override bool InterceptInput(UI.InputEventArgs inputEventArgs)
```

#### Parameters

`inputEventArgs` [UI](Jumbee.Console.UI.md).[InputEventArgs](Jumbee.Console.UI.InputEventArgs.md)

#### Returns

 bool

### <a id="Jumbee_Console_Overlay_Reanchor_System_Int32_System_Int32_"></a> Reanchor\(int, int\)

Re-anchors the current (non-passive) popup at (<code class="paramref">x</code>, <code class="paramref">y</code>) without
    touching focus.

```csharp
public void Reanchor(int x, int y)
```

#### Parameters

`x` int

`y` int

#### Remarks

A popup that changes its own size while open (e.g. a <xref href="Jumbee.Console.ContextMenu" data-throw-if-not-resolved="false"></xref> opening a submenu)
    calls this so the overlay re-measures and re-lays-out the popup at its new size.

### <a id="Jumbee_Console_Overlay_Show_Jumbee_Console_Control_"></a> Show\(Control\)

Show <code class="paramref">popup</code> centered over the bottom layer.

```csharp
public void Show(Control popup)
```

#### Parameters

`popup` [Control](Jumbee.Console.Control.md)

### <a id="Jumbee_Console_Overlay_Show_Jumbee_Console_Control_System_Int32_System_Int32_"></a> Show\(Control, int, int\)

Show <code class="paramref">popup</code> with its top-left anchored at (<code class="paramref">x</code>, <code class="paramref">y</code>).

```csharp
public void Show(Control popup, int x, int y)
```

#### Parameters

`popup` [Control](Jumbee.Console.Control.md)

`x` int

`y` int

### <a id="Jumbee_Console_Overlay_ShowModal_Jumbee_Console_Control_"></a> ShowModal\(Control\)

Show <code class="paramref">popup</code> centered over a click-blocking scrim. The layer beneath cannot be clicked
and the popup stays open until closed explicitly or via <xref href="Jumbee.Console.Overlay.CloseKey" data-throw-if-not-resolved="false"></xref>.

```csharp
public void ShowModal(Control popup)
```

#### Parameters

`popup` [Control](Jumbee.Console.Control.md)

### <a id="Jumbee_Console_Overlay_ShowPassive_Jumbee_Console_Control_System_Int32_System_Int32_"></a> ShowPassive\(Control, int, int\)

Shows <code class="paramref">popup</code> anchored at (<code class="paramref">x</code>, <code class="paramref">y</code>) as a <em>passive</em>
layer: it is drawn over (and is mouse-clickable), but does NOT take focus and does NOT capture keyboard
routing/navigation — the layer beneath keeps focus and keeps receiving keys.

```csharp
public void ShowPassive(Control popup, int x, int y)
```

#### Parameters

`popup` [Control](Jumbee.Console.Control.md)

`x` int

`y` int

#### Remarks

The caller owns dismissal (e.g. an <xref href="Jumbee.Console.Autocomplete" data-throw-if-not-resolved="false"></xref> popup that floats under a still-focused text
field). Use <xref href="Jumbee.Console.Overlay.Hide" data-throw-if-not-resolved="false"></xref> to close it.

