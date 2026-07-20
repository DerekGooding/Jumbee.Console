# <a id="Jumbee_Console_ControlExtensions"></a> Class ControlExtensions

Namespace: [Jumbee.Console](Jumbee.Console.md)  
Assembly: Jumbee.Console.dll  

Fluent extension helpers for configuring controls, frames, and geometry values.

```csharp
public static class ControlExtensions
```

#### Inheritance

object ← 
[ControlExtensions](Jumbee.Console.ControlExtensions.md)

## Methods

### <a id="Jumbee_Console_ControlExtensions_Add_ConsoleGUI_Space_Position_System_Int32_System_Int32_"></a> Add\(Position, int, int\)

Returns <code class="paramref">position</code> offset by <code class="paramref">x</code> columns and <code class="paramref">y</code> rows.

```csharp
public static Position Add(this Position position, int x, int y)
```

#### Parameters

`position` Position

`x` int

`y` int

#### Returns

 Position

### <a id="Jumbee_Console_ControlExtensions_Deconstruct_ConsoleGUI_Space_Position_System_Int32__System_Int32__"></a> Deconstruct\(Position, out int, out int\)

Deconstructs a <xref href="ConsoleGUI.Space.Position" data-throw-if-not-resolved="false"></xref> into its <code class="paramref">X</code> and <code class="paramref">Y</code> components.

```csharp
public static void Deconstruct(this Position position, out int X, out int Y)
```

#### Parameters

`position` Position

`X` int

`Y` int

### <a id="Jumbee_Console_ControlExtensions_SubtractClamp_ConsoleGUI_Space_Position_ConsoleGUI_Space_Position_"></a> SubtractClamp\(Position, Position\)

Subtracts <code class="paramref">position2</code> from <code class="paramref">position1</code>, clamping each axis at zero.

```csharp
public static Position SubtractClamp(this Position position1, Position position2)
```

#### Parameters

`position1` Position

`position2` Position

#### Returns

 Position

### <a id="Jumbee_Console_ControlExtensions_SubtractWidth_ConsoleGUI_Space_Size_System_Int32_"></a> SubtractWidth\(Size, int\)

Returns <code class="paramref">size</code> with its width reduced by <code class="paramref">width</code>.

```csharp
public static Size SubtractWidth(this Size size, int width)
```

#### Parameters

`size` Size

`width` int

#### Returns

 Size

### <a id="Jumbee_Console_ControlExtensions_WithAsciiBorder__1___0_System_Nullable_Jumbee_Console_Color__System_Nullable_Jumbee_Console_Color__"></a> WithAsciiBorder<T\>\(T, Color?, Color?\)

Applies an ASCII border with optional colours and returns the control.

```csharp
public static T WithAsciiBorder<T>(this T control, Color? borderFgColor = null, Color? borderBgColor = null) where T : Control
```

#### Parameters

`control` T

`borderFgColor` [Color](Jumbee.Console.Color.md)?

`borderBgColor` [Color](Jumbee.Console.Color.md)?

#### Returns

 T

#### Type Parameters

`T` 

### <a id="Jumbee_Console_ControlExtensions_WithBorder__1___0_System_Nullable_Jumbee_Console_BorderStyle__System_Nullable_Jumbee_Console_Color__System_Nullable_Jumbee_Console_Color__System_Nullable_Jumbee_Console_BorderPlacement__"></a> WithBorder<T\>\(T, BorderStyle?, Color?, Color?, BorderPlacement?\)

Sets the frame's border style, colours, and placement (creating a frame if needed) and returns the control.

```csharp
public static T WithBorder<T>(this T control, BorderStyle? style, Color? borderFgColor = null, Color? borderBgColor = null, BorderPlacement? borderPlacement = null) where T : Control
```

#### Parameters

`control` T

`style` [BorderStyle](Jumbee.Console.BorderStyle.md)?

`borderFgColor` [Color](Jumbee.Console.Color.md)?

`borderBgColor` [Color](Jumbee.Console.Color.md)?

`borderPlacement` [BorderPlacement](Jumbee.Console.BorderPlacement.md)?

#### Returns

 T

#### Type Parameters

`T` 

### <a id="Jumbee_Console_ControlExtensions_WithDoubleBorder__1___0_System_Nullable_Jumbee_Console_Color__System_Nullable_Jumbee_Console_Color__"></a> WithDoubleBorder<T\>\(T, Color?, Color?\)

Applies a double-line border with optional colours and returns the control.

```csharp
public static T WithDoubleBorder<T>(this T control, Color? borderFgColor = null, Color? borderBgColor = null) where T : Control
```

#### Parameters

`control` T

`borderFgColor` [Color](Jumbee.Console.Color.md)?

`borderBgColor` [Color](Jumbee.Console.Color.md)?

#### Returns

 T

#### Type Parameters

`T` 

### <a id="Jumbee_Console_ControlExtensions_WithFrame__1___0_Jumbee_Console_ControlFrame_"></a> WithFrame<T\>\(T, ControlFrame\)

Sets the control's <xref href="Jumbee.Console.Control.Frame" data-throw-if-not-resolved="false"></xref> to <code class="paramref">frame</code> and returns it.

```csharp
public static T WithFrame<T>(this T control, ControlFrame frame) where T : Control
```

#### Parameters

`control` T

`frame` [ControlFrame](Jumbee.Console.ControlFrame.md)

#### Returns

 T

#### Type Parameters

`T` 

### <a id="Jumbee_Console_ControlExtensions_WithFrame__1___0_System_Nullable_Jumbee_Console_BorderStyle__System_Nullable_ConsoleGUI_Space_Offset__System_Nullable_Jumbee_Console_Color__System_Nullable_Jumbee_Console_Color__System_String_System_Nullable_Jumbee_Console_Color__System_Nullable_Jumbee_Console_Color__System_Nullable_Jumbee_Console_BorderPlacement__System_Nullable_Jumbee_Console_BorderStyle__"></a> WithFrame<T\>\(T, BorderStyle?, Offset?, Color?, Color?, string?, Color?, Color?, BorderPlacement?, BorderStyle?\)

Creates or updates the control's <xref href="Jumbee.Console.ControlFrame" data-throw-if-not-resolved="false"></xref> from the supplied border, margin, colour,
    and title options (only supplied arguments are applied) and returns the control.

```csharp
public static T WithFrame<T>(this T control, BorderStyle? borderStyle = null, Offset? margin = null, Color? fgColor = null, Color? bgColor = null, string? title = null, Color? borderFgColor = null, Color? borderBgColor = null, BorderPlacement? borderPlacement = null, BorderStyle? focusedBorderStyle = null) where T : Control
```

#### Parameters

`control` T

`borderStyle` [BorderStyle](Jumbee.Console.BorderStyle.md)?

`margin` Offset?

`fgColor` [Color](Jumbee.Console.Color.md)?

`bgColor` [Color](Jumbee.Console.Color.md)?

`title` string?

`borderFgColor` [Color](Jumbee.Console.Color.md)?

`borderBgColor` [Color](Jumbee.Console.Color.md)?

`borderPlacement` [BorderPlacement](Jumbee.Console.BorderPlacement.md)?

`focusedBorderStyle` [BorderStyle](Jumbee.Console.BorderStyle.md)?

#### Returns

 T

#### Type Parameters

`T` 

### <a id="Jumbee_Console_ControlExtensions_WithHeavyBorder__1___0_System_Nullable_Jumbee_Console_Color__System_Nullable_Jumbee_Console_Color__"></a> WithHeavyBorder<T\>\(T, Color?, Color?\)

Applies a heavy-line border with optional colours and returns the control.

```csharp
public static T WithHeavyBorder<T>(this T control, Color? borderFgColor = null, Color? borderBgColor = null) where T : Control
```

#### Parameters

`control` T

`borderFgColor` [Color](Jumbee.Console.Color.md)?

`borderBgColor` [Color](Jumbee.Console.Color.md)?

#### Returns

 T

#### Type Parameters

`T` 

### <a id="Jumbee_Console_ControlExtensions_WithHeight__1___0_System_Int32_"></a> WithHeight<T\>\(T, int\)

Sets the control's <xref href="Jumbee.Console.Control.Height" data-throw-if-not-resolved="false"></xref> and returns it.

```csharp
public static T WithHeight<T>(this T control, int height) where T : Control
```

#### Parameters

`control` T

`height` int

#### Returns

 T

#### Type Parameters

`T` 

### <a id="Jumbee_Console_ControlExtensions_WithMargin__1___0_System_Int32_System_Int32_System_Int32_System_Int32_"></a> WithMargin<T\>\(T, int, int, int, int\)

Sets the frame's margin to the given left/top/right/bottom offsets (creating a frame if needed) and returns the control.

```csharp
public static T WithMargin<T>(this T control, int left, int top, int right, int bottom) where T : Control
```

#### Parameters

`control` T

`left` int

`top` int

`right` int

`bottom` int

#### Returns

 T

#### Type Parameters

`T` 

### <a id="Jumbee_Console_ControlExtensions_WithMargin__1___0_System_Int32_"></a> WithMargin<T\>\(T, int\)

Sets a uniform frame margin of <code class="paramref">offset</code> on all sides and returns the control.

```csharp
public static T WithMargin<T>(this T control, int offset) where T : Control
```

#### Parameters

`control` T

`offset` int

#### Returns

 T

#### Type Parameters

`T` 

### <a id="Jumbee_Console_ControlExtensions_WithNoBorder__1___0_"></a> WithNoBorder<T\>\(T\)

Removes the frame's border (<xref href="Jumbee.Console.BorderStyle.None" data-throw-if-not-resolved="false"></xref>) and returns the control.

```csharp
public static T WithNoBorder<T>(this T control) where T : Control
```

#### Parameters

`control` T

#### Returns

 T

#### Type Parameters

`T` 

### <a id="Jumbee_Console_ControlExtensions_WithRoundedBorder__1___0_System_Nullable_Jumbee_Console_Color__System_Nullable_Jumbee_Console_Color__"></a> WithRoundedBorder<T\>\(T, Color?, Color?\)

Applies a rounded border with optional colours and returns the control.

```csharp
public static T WithRoundedBorder<T>(this T control, Color? borderFgColor = null, Color? borderBgColor = null) where T : Control
```

#### Parameters

`control` T

`borderFgColor` [Color](Jumbee.Console.Color.md)?

`borderBgColor` [Color](Jumbee.Console.Color.md)?

#### Returns

 T

#### Type Parameters

`T` 

### <a id="Jumbee_Console_ControlExtensions_WithScrollBarGlyphs__1___0_Jumbee_Console_ScrollBarGlyphs_"></a> WithScrollBarGlyphs<T\>\(T, ScrollBarGlyphs\)

Sets the frame's <xref href="Jumbee.Console.ScrollBarGlyphs" data-throw-if-not-resolved="false"></xref> (creating a frame if needed) and returns the control.

```csharp
public static T WithScrollBarGlyphs<T>(this T control, ScrollBarGlyphs glyphs) where T : Control
```

#### Parameters

`control` T

`glyphs` [ScrollBarGlyphs](Jumbee.Console.ScrollBarGlyphs.md)

#### Returns

 T

#### Type Parameters

`T` 

### <a id="Jumbee_Console_ControlExtensions_WithScrollBarStyle__1___0_Jumbee_Console_ScrollBarStyle_"></a> WithScrollBarStyle<T\>\(T, ScrollBarStyle\)

Sets the frame's <xref href="Jumbee.Console.ScrollBarStyle" data-throw-if-not-resolved="false"></xref> (creating a frame if needed) and returns the control.

```csharp
public static T WithScrollBarStyle<T>(this T control, ScrollBarStyle style) where T : Control
```

#### Parameters

`control` T

`style` [ScrollBarStyle](Jumbee.Console.ScrollBarStyle.md)

#### Returns

 T

#### Type Parameters

`T` 

### <a id="Jumbee_Console_ControlExtensions_WithSize__1___0_System_Nullable_System_Int32__System_Nullable_System_Int32__"></a> WithSize<T\>\(T, int?, int?\)

Sets the control's width and/or height (at least one must be supplied) and returns it.

```csharp
public static T WithSize<T>(this T control, int? width = null, int? height = null) where T : Control
```

#### Parameters

`control` T

`width` int?

`height` int?

#### Returns

 T

#### Type Parameters

`T` 

### <a id="Jumbee_Console_ControlExtensions_WithSquareBorder__1___0_System_Nullable_Jumbee_Console_Color__System_Nullable_Jumbee_Console_Color__"></a> WithSquareBorder<T\>\(T, Color?, Color?\)

Applies a square border with optional colours and returns the control.

```csharp
public static T WithSquareBorder<T>(this T control, Color? borderFgColor = null, Color? borderBgColor = null) where T : Control
```

#### Parameters

`control` T

`borderFgColor` [Color](Jumbee.Console.Color.md)?

`borderBgColor` [Color](Jumbee.Console.Color.md)?

#### Returns

 T

#### Type Parameters

`T` 

### <a id="Jumbee_Console_ControlExtensions_WithStyle_System_String_Jumbee_Console_Style_"></a> WithStyle\(string, Style\)

Wraps <code class="paramref">s</code> in a Spectre <xref href="Spectre.Console.Markup" data-throw-if-not-resolved="false"></xref> using <code class="paramref">style</code>.

```csharp
public static Markup WithStyle(this string s, Style style)
```

#### Parameters

`s` string

`style` [Style](Jumbee.Console.Style.md)

#### Returns

 Markup

### <a id="Jumbee_Console_ControlExtensions_WithTitle__1___0_System_String_"></a> WithTitle<T\>\(T, string\)

Sets the frame's title (creating a frame if needed) and returns the control.

```csharp
public static T WithTitle<T>(this T control, string title) where T : Control
```

#### Parameters

`control` T

`title` string

#### Returns

 T

#### Type Parameters

`T` 

### <a id="Jumbee_Console_ControlExtensions_WithTitle__1___0_System_String_Jumbee_Console_TitleStyle_"></a> WithTitle<T\>\(T, string, TitleStyle\)

Sets the frame's title and <xref href="Jumbee.Console.TitleStyle" data-throw-if-not-resolved="false"></xref> (creating a frame if needed) and returns the control.

```csharp
public static T WithTitle<T>(this T control, string title, TitleStyle titleStyle) where T : Control
```

#### Parameters

`control` T

`title` string

`titleStyle` [TitleStyle](Jumbee.Console.TitleStyle.md)

#### Returns

 T

#### Type Parameters

`T` 

### <a id="Jumbee_Console_ControlExtensions_WithTitle__1___0_System_String_Jumbee_Console_TitlePos_Jumbee_Console_TitleBorderStyle_Jumbee_Console_TitleColorStyle_"></a> WithTitle<T\>\(T, string, TitlePos, TitleBorderStyle, TitleColorStyle\)

Sets the frame's title with the given position, border style, and colour style (creating a frame if needed) and returns the control.

```csharp
public static T WithTitle<T>(this T control, string title, TitlePos pos, TitleBorderStyle borderStyle, TitleColorStyle color) where T : Control
```

#### Parameters

`control` T

`title` string

`pos` [TitlePos](Jumbee.Console.TitlePos.md)

`borderStyle` [TitleBorderStyle](Jumbee.Console.TitleBorderStyle.md)

`color` [TitleColorStyle](Jumbee.Console.TitleColorStyle.md)

#### Returns

 T

#### Type Parameters

`T` 

### <a id="Jumbee_Console_ControlExtensions_WithWidth__1___0_System_Int32_"></a> WithWidth<T\>\(T, int\)

Sets the control's <xref href="Jumbee.Console.Control.Width" data-throw-if-not-resolved="false"></xref> and returns it.

```csharp
public static T WithWidth<T>(this T control, int width) where T : Control
```

#### Parameters

`control` T

`width` int

#### Returns

 T

#### Type Parameters

`T` 

