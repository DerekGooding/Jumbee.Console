# <a id="Jumbee_Console_ScrollBarStyle"></a> Struct ScrollBarStyle

Namespace: [Jumbee.Console](Jumbee.Console.md)  
Assembly: Jumbee.Console.Styles.dll  

The per-part <xref href="Jumbee.Console.Style" data-throw-if-not-resolved="false"></xref> (foreground/background/decoration, no glyph) a control frame applies to its
vertical scrollbar.

```csharp
public readonly struct ScrollBarStyle
```

## Remarks

The glyphs come separately from <xref href="Jumbee.Console.ScrollBarGlyphs" data-throw-if-not-resolved="false"></xref> (via
    <xref href="Jumbee.Console.IGlyphTheme.ScrollBar" data-throw-if-not-resolved="false"></xref>); a control frame composes the two into its scrollbar cells.

## Constructors

### <a id="Jumbee_Console_ScrollBarStyle__ctor_Jumbee_Console_Style_Jumbee_Console_Style_Jumbee_Console_Style_Jumbee_Console_Style_"></a> ScrollBarStyle\(Style, Style, Style, Style\)

Initializes a new <xref href="Jumbee.Console.ScrollBarStyle" data-throw-if-not-resolved="false"></xref> from the thumb, track, and end-arrow styles.

```csharp
public ScrollBarStyle(Style thumb, Style track, Style upArrow, Style downArrow)
```

#### Parameters

`thumb` [Style](Jumbee.Console.Style.md)

`track` [Style](Jumbee.Console.Style.md)

`upArrow` [Style](Jumbee.Console.Style.md)

`downArrow` [Style](Jumbee.Console.Style.md)

## Properties

### <a id="Jumbee_Console_ScrollBarStyle_Default"></a> Default

The default colours: a medium-grey thumb on a dim dark-grey track (a neutral, modern groove that
    reads clearly under the smooth block bar), with terminal-default arrows for the classic bar.

```csharp
public static ScrollBarStyle Default { get; }
```

#### Property Value

 [ScrollBarStyle](Jumbee.Console.ScrollBarStyle.md)

### <a id="Jumbee_Console_ScrollBarStyle_DownArrow"></a> DownArrow

Style for the bottom end arrow.

```csharp
public Style DownArrow { get; init; }
```

#### Property Value

 [Style](Jumbee.Console.Style.md)

### <a id="Jumbee_Console_ScrollBarStyle_Thumb"></a> Thumb

Style for the thumb (the draggable handle).

```csharp
public Style Thumb { get; init; }
```

#### Property Value

 [Style](Jumbee.Console.Style.md)

### <a id="Jumbee_Console_ScrollBarStyle_Track"></a> Track

Style for the track behind the thumb.

```csharp
public Style Track { get; init; }
```

#### Property Value

 [Style](Jumbee.Console.Style.md)

### <a id="Jumbee_Console_ScrollBarStyle_UpArrow"></a> UpArrow

Style for the top end arrow.

```csharp
public Style UpArrow { get; init; }
```

#### Property Value

 [Style](Jumbee.Console.Style.md)

## Methods

### <a id="Jumbee_Console_ScrollBarStyle_Equals_Jumbee_Console_ScrollBarStyle_"></a> Equals\(ScrollBarStyle\)

Determines whether this <xref href="Jumbee.Console.ScrollBarStyle" data-throw-if-not-resolved="false"></xref> equals <code class="paramref">other</code>.

```csharp
public bool Equals(ScrollBarStyle other)
```

#### Parameters

`other` [ScrollBarStyle](Jumbee.Console.ScrollBarStyle.md)

#### Returns

 bool

### <a id="Jumbee_Console_ScrollBarStyle_Equals_System_Object_"></a> Equals\(object?\)

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

### <a id="Jumbee_Console_ScrollBarStyle_GetHashCode"></a> GetHashCode\(\)

Returns the hash code for this instance.

```csharp
public override int GetHashCode()
```

#### Returns

 int

A 32-bit signed integer that is the hash code for this instance.

### <a id="Jumbee_Console_ScrollBarStyle_Uniform_Jumbee_Console_Style_"></a> Uniform\(Style\)

A scrollbar style with the same <xref href="Jumbee.Console.Style" data-throw-if-not-resolved="false"></xref> applied to every part.

```csharp
public static ScrollBarStyle Uniform(Style style)
```

#### Parameters

`style` [Style](Jumbee.Console.Style.md)

#### Returns

 [ScrollBarStyle](Jumbee.Console.ScrollBarStyle.md)

### <a id="Jumbee_Console_ScrollBarStyle_WithColors_System_Nullable_Jumbee_Console_Color__System_Nullable_Jumbee_Console_Color__System_Nullable_Jumbee_Console_Color__"></a> WithColors\(Color?, Color?, Color?\)

Returns a copy with the foreground colours overridden. A <code>null</code> argument leaves that part unchanged;
<code class="paramref">arrows</code> recolours both end arrows.

```csharp
public ScrollBarStyle WithColors(Color? thumb = null, Color? track = null, Color? arrows = null)
```

#### Parameters

`thumb` [Color](Jumbee.Console.Color.md)?

`track` [Color](Jumbee.Console.Color.md)?

`arrows` [Color](Jumbee.Console.Color.md)?

#### Returns

 [ScrollBarStyle](Jumbee.Console.ScrollBarStyle.md)

## Operators

### <a id="Jumbee_Console_ScrollBarStyle_op_Equality_Jumbee_Console_ScrollBarStyle_Jumbee_Console_ScrollBarStyle_"></a> operator ==\(ScrollBarStyle, ScrollBarStyle\)

Equality operator.

```csharp
public static bool operator ==(ScrollBarStyle a, ScrollBarStyle b)
```

#### Parameters

`a` [ScrollBarStyle](Jumbee.Console.ScrollBarStyle.md)

`b` [ScrollBarStyle](Jumbee.Console.ScrollBarStyle.md)

#### Returns

 bool

### <a id="Jumbee_Console_ScrollBarStyle_op_Inequality_Jumbee_Console_ScrollBarStyle_Jumbee_Console_ScrollBarStyle_"></a> operator \!=\(ScrollBarStyle, ScrollBarStyle\)

Inequality operator.

```csharp
public static bool operator !=(ScrollBarStyle a, ScrollBarStyle b)
```

#### Parameters

`a` [ScrollBarStyle](Jumbee.Console.ScrollBarStyle.md)

`b` [ScrollBarStyle](Jumbee.Console.ScrollBarStyle.md)

#### Returns

 bool

