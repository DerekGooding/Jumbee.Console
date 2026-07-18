# <a id="Jumbee_Console_ButtonStyle"></a> Struct ButtonStyle

Namespace: [Jumbee.Console](Jumbee.Console.md)  
Assembly: Jumbee.Console.Styles.dll  

The appearance of a <code>Button</code>: its fill <xref href="Jumbee.Console.Style" data-throw-if-not-resolved="false"></xref> in each interaction state (text colour +
background), its <xref href="Jumbee.Console.ButtonStyle.Shape" data-throw-if-not-resolved="false"></xref>, an optional fixed/minimum width, and whether the label
is bold.

```csharp
public readonly struct ButtonStyle
```

## Remarks

A button's default style comes from <xref href="Jumbee.Console.IStyleTheme.PrimaryButton" data-throw-if-not-resolved="false"></xref> /
<xref href="Jumbee.Console.IStyleTheme.SecondaryButton" data-throw-if-not-resolved="false"></xref>; <xref href="Jumbee.Console.ButtonStyle.Primary" data-throw-if-not-resolved="false"></xref>/<xref href="Jumbee.Console.ButtonStyle.Secondary" data-throw-if-not-resolved="false"></xref> here are the
theme-independent fallbacks those tokens default to.

## Constructors

### <a id="Jumbee_Console_ButtonStyle__ctor_Jumbee_Console_Style_Jumbee_Console_Style_Jumbee_Console_Style_Jumbee_Console_ButtonShape_System_Nullable_Jumbee_Console_Color__System_Nullable_Jumbee_Console_Color__System_Boolean_System_Int32_System_Int32_"></a> ButtonStyle\(Style, Style, Style, ButtonShape, Color?, Color?, bool, int, int\)

Initializes a new <xref href="Jumbee.Console.ButtonStyle" data-throw-if-not-resolved="false"></xref> from the per-state fills, shape, bevel colours, bold flag, and width constraints.

```csharp
public ButtonStyle(Style normal, Style hover, Style press, ButtonShape shape = ButtonShape.Flat, Color? bevelLight = null, Color? bevelDark = null, bool bold = true, int width = 0, int minWidth = 0)
```

#### Parameters

`normal` [Style](Jumbee.Console.Style.md)

`hover` [Style](Jumbee.Console.Style.md)

`press` [Style](Jumbee.Console.Style.md)

`shape` [ButtonShape](Jumbee.Console.ButtonShape.md)

`bevelLight` [Color](Jumbee.Console.Color.md)?

`bevelDark` [Color](Jumbee.Console.Color.md)?

`bold` bool

`width` int

`minWidth` int

## Properties

### <a id="Jumbee_Console_ButtonStyle_BevelDark"></a> BevelDark

The bevel's bottom-edge shadow (<xref href="Jumbee.Console.ButtonShape.Modern" data-throw-if-not-resolved="false"></xref>), or <a href="https://learn.microsoft.com/dotnet/csharp/language-reference/keywords/null">null</a> to
    derive it by darkening the fill background.

```csharp
public Color? BevelDark { get; init; }
```

#### Property Value

 [Color](Jumbee.Console.Color.md)?

### <a id="Jumbee_Console_ButtonStyle_BevelLight"></a> BevelLight

The bevel's top-edge highlight (<xref href="Jumbee.Console.ButtonShape.Modern" data-throw-if-not-resolved="false"></xref>), or <a href="https://learn.microsoft.com/dotnet/csharp/language-reference/keywords/null">null</a> to
    derive it by lightening the fill background.

```csharp
public Color? BevelLight { get; init; }
```

#### Property Value

 [Color](Jumbee.Console.Color.md)?

### <a id="Jumbee_Console_ButtonStyle_Bold"></a> Bold

Whether the label is drawn bold. Defaults to <a href="https://learn.microsoft.com/dotnet/csharp/language-reference/builtin-types/bool">true</a>.

```csharp
public bool Bold { get; init; }
```

#### Property Value

 bool

### <a id="Jumbee_Console_ButtonStyle_Hover"></a> Hover

Fill while the pointer is over the button.

```csharp
public Style Hover { get; init; }
```

#### Property Value

 [Style](Jumbee.Console.Style.md)

### <a id="Jumbee_Console_ButtonStyle_IsModern"></a> IsModern

<a href="https://learn.microsoft.com/dotnet/csharp/language-reference/builtin-types/bool">true</a> for the 3-row raised <xref href="Jumbee.Console.ButtonShape.Modern" data-throw-if-not-resolved="false"></xref> shape.

```csharp
public bool IsModern { get; }
```

#### Property Value

 bool

### <a id="Jumbee_Console_ButtonStyle_MinWidth"></a> MinWidth

A minimum outer width in cells (so short labels still read as buttons), or 0 for none.

```csharp
public int MinWidth { get; init; }
```

#### Property Value

 int

### <a id="Jumbee_Console_ButtonStyle_Normal"></a> Normal

Fill (foreground text colour + background) at rest.

```csharp
public Style Normal { get; init; }
```

#### Property Value

 [Style](Jumbee.Console.Style.md)

### <a id="Jumbee_Console_ButtonStyle_Press"></a> Press

Fill while the button is pressed/activated.

```csharp
public Style Press { get; init; }
```

#### Property Value

 [Style](Jumbee.Console.Style.md)

### <a id="Jumbee_Console_ButtonStyle_Primary"></a> Primary

A primary action button: white text on a blue fill (flat by default; use
    <xref href="Jumbee.Console.ButtonShape.Modern" data-throw-if-not-resolved="false"></xref> for the raised look).

```csharp
public static ButtonStyle Primary { get; }
```

#### Property Value

 [ButtonStyle](Jumbee.Console.ButtonStyle.md)

### <a id="Jumbee_Console_ButtonStyle_Secondary"></a> Secondary

A secondary action button: light text on a neutral grey fill.

```csharp
public static ButtonStyle Secondary { get; }
```

#### Property Value

 [ButtonStyle](Jumbee.Console.ButtonStyle.md)

### <a id="Jumbee_Console_ButtonStyle_Shape"></a> Shape

The button's shape. Defaults to <xref href="Jumbee.Console.ButtonShape.Flat" data-throw-if-not-resolved="false"></xref> (a simple single-row button);
    use <xref href="Jumbee.Console.ButtonShape.Modern" data-throw-if-not-resolved="false"></xref> for the raised 3-row look. Changing it re-lays the button out.

```csharp
public ButtonShape Shape { get; init; }
```

#### Property Value

 [ButtonShape](Jumbee.Console.ButtonShape.md)

### <a id="Jumbee_Console_ButtonStyle_Width"></a> Width

A fixed outer width in cells, or 0 to size to the label (subject to <xref href="Jumbee.Console.ButtonStyle.MinWidth" data-throw-if-not-resolved="false"></xref>).

```csharp
public int Width { get; init; }
```

#### Property Value

 int

## Methods

### <a id="Jumbee_Console_ButtonStyle_Equals_Jumbee_Console_ButtonStyle_"></a> Equals\(ButtonStyle\)

Determines whether this <xref href="Jumbee.Console.ButtonStyle" data-throw-if-not-resolved="false"></xref> equals <code class="paramref">other</code>.

```csharp
public bool Equals(ButtonStyle other)
```

#### Parameters

`other` [ButtonStyle](Jumbee.Console.ButtonStyle.md)

#### Returns

 bool

### <a id="Jumbee_Console_ButtonStyle_Equals_System_Object_"></a> Equals\(object?\)

Indicates whether this instance and a specified object are equal.

```csharp
public override bool Equals(object? obj)
```

#### Parameters

`obj` object?

The object to compare with the current instance.

#### Returns

 bool

<a href="https://learn.microsoft.com/dotnet/csharp/language-reference/builtin-types/bool">true</a> if <code class="paramref">obj</code> and this instance are the same type and represent the same value; otherwise, <a href="https://learn.microsoft.com/dotnet/csharp/language-reference/builtin-types/bool">false</a>.

### <a id="Jumbee_Console_ButtonStyle_GetHashCode"></a> GetHashCode\(\)

Returns the hash code for this instance.

```csharp
public override int GetHashCode()
```

#### Returns

 int

A 32-bit signed integer that is the hash code for this instance.

### <a id="Jumbee_Console_ButtonStyle_WithColors_System_Nullable_Jumbee_Console_Style__System_Nullable_Jumbee_Console_Style__System_Nullable_Jumbee_Console_Style__"></a> WithColors\(Style?, Style?, Style?\)

Returns a copy with one or more per-state fills overridden; a <a href="https://learn.microsoft.com/dotnet/csharp/language-reference/keywords/null">null</a> argument leaves
    that state unchanged.

```csharp
public ButtonStyle WithColors(Style? normal = null, Style? hover = null, Style? press = null)
```

#### Parameters

`normal` [Style](Jumbee.Console.Style.md)?

`hover` [Style](Jumbee.Console.Style.md)?

`press` [Style](Jumbee.Console.Style.md)?

#### Returns

 [ButtonStyle](Jumbee.Console.ButtonStyle.md)

### <a id="Jumbee_Console_ButtonStyle_WithShape_Jumbee_Console_ButtonShape_"></a> WithShape\(ButtonShape\)

Returns a copy with a different shape (e.g. opting into <xref href="Jumbee.Console.ButtonShape.Modern" data-throw-if-not-resolved="false"></xref>).

```csharp
public ButtonStyle WithShape(ButtonShape shape)
```

#### Parameters

`shape` [ButtonShape](Jumbee.Console.ButtonShape.md)

#### Returns

 [ButtonStyle](Jumbee.Console.ButtonStyle.md)

### <a id="Jumbee_Console_ButtonStyle_WithWidth_System_Int32_"></a> WithWidth\(int\)

Returns a copy with a fixed outer <code class="paramref">width</code> (0 = size to the label).

```csharp
public ButtonStyle WithWidth(int width)
```

#### Parameters

`width` int

#### Returns

 [ButtonStyle](Jumbee.Console.ButtonStyle.md)

## Operators

### <a id="Jumbee_Console_ButtonStyle_op_Equality_Jumbee_Console_ButtonStyle_Jumbee_Console_ButtonStyle_"></a> operator ==\(ButtonStyle, ButtonStyle\)

Equality operator.

```csharp
public static bool operator ==(ButtonStyle a, ButtonStyle b)
```

#### Parameters

`a` [ButtonStyle](Jumbee.Console.ButtonStyle.md)

`b` [ButtonStyle](Jumbee.Console.ButtonStyle.md)

#### Returns

 bool

### <a id="Jumbee_Console_ButtonStyle_op_Inequality_Jumbee_Console_ButtonStyle_Jumbee_Console_ButtonStyle_"></a> operator \!=\(ButtonStyle, ButtonStyle\)

Inequality operator.

```csharp
public static bool operator !=(ButtonStyle a, ButtonStyle b)
```

#### Parameters

`a` [ButtonStyle](Jumbee.Console.ButtonStyle.md)

`b` [ButtonStyle](Jumbee.Console.ButtonStyle.md)

#### Returns

 bool

