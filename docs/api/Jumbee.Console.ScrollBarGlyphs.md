# <a id="Jumbee_Console_ScrollBarGlyphs"></a> Struct ScrollBarGlyphs

Namespace: [Jumbee.Console](Jumbee.Console.md)  
Assembly: Jumbee.Console.Styles.dll  

The glyphs (no colours) a control frame's vertical scrollbar draws for each part: the moving thumb, the track
behind it, and the two end arrows.

```csharp
public readonly struct ScrollBarGlyphs
```

## Remarks

<p>
Colours/decorations come separately from <xref href="Jumbee.Console.ScrollBarStyle" data-throw-if-not-resolved="false"></xref> (via <xref href="Jumbee.Console.IStyleTheme.ScrollBar" data-throw-if-not-resolved="false"></xref>);
a control frame composes the two into its scrollbar cells. The static presets are convenience helpers for
restyling a single control's glyphs (e.g. via WithScrollBarGlyphs).
</p>
<p><xref href="Jumbee.Console.ScrollBarGlyphs.Mode" data-throw-if-not-resolved="false"></xref> selects the rendering: <xref href="Jumbee.Console.ScrollBarMode.Smooth" data-throw-if-not-resolved="false"></xref> (the default) ignores the
glyph strings and draws a sub-cell block bar; <xref href="Jumbee.Console.ScrollBarMode.Classic" data-throw-if-not-resolved="false"></xref> uses the four glyphs below.</p>

## Constructors

### <a id="Jumbee_Console_ScrollBarGlyphs__ctor_System_String_System_String_System_String_System_String_"></a> ScrollBarGlyphs\(string, string, string, string\)

Builds a <xref href="Jumbee.Console.ScrollBarMode.Classic" data-throw-if-not-resolved="false"></xref> glyph set (explicit glyphs imply the classic bar).

```csharp
public ScrollBarGlyphs(string thumb, string track, string upArrow, string downArrow)
```

#### Parameters

`thumb` string

`track` string

`upArrow` string

`downArrow` string

## Properties

### <a id="Jumbee_Console_ScrollBarGlyphs_Block"></a> Block

A solid block thumb on a light vertical-line track with triangle arrows (classic).

```csharp
public static ScrollBarGlyphs Block { get; }
```

#### Property Value

 [ScrollBarGlyphs](Jumbee.Console.ScrollBarGlyphs.md)

### <a id="Jumbee_Console_ScrollBarGlyphs_Classic"></a> Classic

The original legacy glyphs for terminals without block support: a '#' thumb on a '|' track with
    triangle arrows (<xref href="Jumbee.Console.ScrollBarMode.Classic" data-throw-if-not-resolved="false"></xref>).

```csharp
public static ScrollBarGlyphs Classic { get; }
```

#### Property Value

 [ScrollBarGlyphs](Jumbee.Console.ScrollBarGlyphs.md)

### <a id="Jumbee_Console_ScrollBarGlyphs_Default"></a> Default

The default: the modern <xref href="Jumbee.Console.ScrollBarMode.Smooth" data-throw-if-not-resolved="false"></xref> block bar (sub-cell thumb, no arrows).

```csharp
public static ScrollBarGlyphs Default { get; }
```

#### Property Value

 [ScrollBarGlyphs](Jumbee.Console.ScrollBarGlyphs.md)

### <a id="Jumbee_Console_ScrollBarGlyphs_DownArrow"></a> DownArrow

The glyph at the bottom end of the scrollbar. Classic mode only.

```csharp
public string DownArrow { get; init; }
```

#### Property Value

 string

### <a id="Jumbee_Console_ScrollBarGlyphs_Line"></a> Line

A heavy vertical-line thumb on a light vertical-line track with triangle arrows (classic).

```csharp
public static ScrollBarGlyphs Line { get; }
```

#### Property Value

 [ScrollBarGlyphs](Jumbee.Console.ScrollBarGlyphs.md)

### <a id="Jumbee_Console_ScrollBarGlyphs_Mode"></a> Mode

Which bar to render (smooth sub-cell block bar, or the classic three-part bar). Defaults to
    <xref href="Jumbee.Console.ScrollBarMode.Smooth" data-throw-if-not-resolved="false"></xref> for a default-constructed value and for the <xref href="Jumbee.Console.ScrollBarGlyphs.Smooth" data-throw-if-not-resolved="false"></xref> preset.

```csharp
public ScrollBarMode Mode { get; init; }
```

#### Property Value

 [ScrollBarMode](Jumbee.Console.ScrollBarMode.md)

### <a id="Jumbee_Console_ScrollBarGlyphs_Shaded"></a> Shaded

A shaded (dithered) thumb on a light vertical-line track with thin line arrows (classic).

```csharp
public static ScrollBarGlyphs Shaded { get; }
```

#### Property Value

 [ScrollBarGlyphs](Jumbee.Console.ScrollBarGlyphs.md)

### <a id="Jumbee_Console_ScrollBarGlyphs_Smooth"></a> Smooth

The modern <xref href="Jumbee.Console.ScrollBarMode.Smooth" data-throw-if-not-resolved="false"></xref> block bar. The glyph fields are placeholders (a solid
    block) and unused by the smooth renderer, which draws its own eighth-block cells.

```csharp
public static ScrollBarGlyphs Smooth { get; }
```

#### Property Value

 [ScrollBarGlyphs](Jumbee.Console.ScrollBarGlyphs.md)

### <a id="Jumbee_Console_ScrollBarGlyphs_Thumb"></a> Thumb

The glyph for the part of the track currently in view (the draggable handle). Classic mode only.

```csharp
public string Thumb { get; init; }
```

#### Property Value

 string

### <a id="Jumbee_Console_ScrollBarGlyphs_Track"></a> Track

The glyph for the track behind the thumb. Classic mode only.

```csharp
public string Track { get; init; }
```

#### Property Value

 string

### <a id="Jumbee_Console_ScrollBarGlyphs_UpArrow"></a> UpArrow

The glyph at the top end of the scrollbar. Classic mode only.

```csharp
public string UpArrow { get; init; }
```

#### Property Value

 string

## Methods

### <a id="Jumbee_Console_ScrollBarGlyphs_Equals_Jumbee_Console_ScrollBarGlyphs_"></a> Equals\(ScrollBarGlyphs\)

Determines whether this <xref href="Jumbee.Console.ScrollBarGlyphs" data-throw-if-not-resolved="false"></xref> equals <code class="paramref">other</code>.

```csharp
public bool Equals(ScrollBarGlyphs other)
```

#### Parameters

`other` [ScrollBarGlyphs](Jumbee.Console.ScrollBarGlyphs.md)

#### Returns

 bool

### <a id="Jumbee_Console_ScrollBarGlyphs_Equals_System_Object_"></a> Equals\(object?\)

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

### <a id="Jumbee_Console_ScrollBarGlyphs_GetHashCode"></a> GetHashCode\(\)

Returns the hash code for this instance.

```csharp
public override int GetHashCode()
```

#### Returns

 int

A 32-bit signed integer that is the hash code for this instance.

## Operators

### <a id="Jumbee_Console_ScrollBarGlyphs_op_Equality_Jumbee_Console_ScrollBarGlyphs_Jumbee_Console_ScrollBarGlyphs_"></a> operator ==\(ScrollBarGlyphs, ScrollBarGlyphs\)

Equality operator.

```csharp
public static bool operator ==(ScrollBarGlyphs a, ScrollBarGlyphs b)
```

#### Parameters

`a` [ScrollBarGlyphs](Jumbee.Console.ScrollBarGlyphs.md)

`b` [ScrollBarGlyphs](Jumbee.Console.ScrollBarGlyphs.md)

#### Returns

 bool

### <a id="Jumbee_Console_ScrollBarGlyphs_op_Inequality_Jumbee_Console_ScrollBarGlyphs_Jumbee_Console_ScrollBarGlyphs_"></a> operator \!=\(ScrollBarGlyphs, ScrollBarGlyphs\)

Inequality operator.

```csharp
public static bool operator !=(ScrollBarGlyphs a, ScrollBarGlyphs b)
```

#### Parameters

`a` [ScrollBarGlyphs](Jumbee.Console.ScrollBarGlyphs.md)

`b` [ScrollBarGlyphs](Jumbee.Console.ScrollBarGlyphs.md)

#### Returns

 bool

