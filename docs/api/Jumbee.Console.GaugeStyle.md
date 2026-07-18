# <a id="Jumbee_Console_GaugeStyle"></a> Struct GaugeStyle

Namespace: [Jumbee.Console](Jumbee.Console.md)  
Assembly: Jumbee.Console.Styles.dll  

The per-part <xref href="Jumbee.Console.Style" data-throw-if-not-resolved="false"></xref> a <code>Gauge</code> composes: the filled portion of the bar,
the empty track behind it, and the percent/value readout (and any inline label).

```csharp
public readonly struct GaugeStyle
```

## Remarks

Only the foreground colour of <xref href="Jumbee.Console.GaugeStyle.Fill" data-throw-if-not-resolved="false"></xref>/<xref href="Jumbee.Console.GaugeStyle.Track" data-throw-if-not-resolved="false"></xref> is used — the bar is drawn as a
    solid colour band.

## Constructors

### <a id="Jumbee_Console_GaugeStyle__ctor_Jumbee_Console_Style_Jumbee_Console_Style_Jumbee_Console_Style_"></a> GaugeStyle\(Style, Style, Style\)

Initializes a new <xref href="Jumbee.Console.GaugeStyle" data-throw-if-not-resolved="false"></xref> from the fill, track, and text styles.

```csharp
public GaugeStyle(Style fill, Style track, Style text)
```

#### Parameters

`fill` [Style](Jumbee.Console.Style.md)

`track` [Style](Jumbee.Console.Style.md)

`text` [Style](Jumbee.Console.Style.md)

## Properties

### <a id="Jumbee_Console_GaugeStyle_Default"></a> Default

A blue fill on a dim dark-grey track, with grey text.

```csharp
public static GaugeStyle Default { get; }
```

#### Property Value

 [GaugeStyle](Jumbee.Console.GaugeStyle.md)

### <a id="Jumbee_Console_GaugeStyle_Fill"></a> Fill

The filled portion of the bar (its foreground colour fills the band).

```csharp
public Style Fill { get; init; }
```

#### Property Value

 [Style](Jumbee.Console.Style.md)

### <a id="Jumbee_Console_GaugeStyle_Text"></a> Text

The percent/value readout and any inline label.

```csharp
public Style Text { get; init; }
```

#### Property Value

 [Style](Jumbee.Console.Style.md)

### <a id="Jumbee_Console_GaugeStyle_Track"></a> Track

The empty track behind the fill (its foreground colour fills the band).

```csharp
public Style Track { get; init; }
```

#### Property Value

 [Style](Jumbee.Console.Style.md)

## Methods

### <a id="Jumbee_Console_GaugeStyle_Equals_Jumbee_Console_GaugeStyle_"></a> Equals\(GaugeStyle\)

Determines whether this <xref href="Jumbee.Console.GaugeStyle" data-throw-if-not-resolved="false"></xref> equals <code class="paramref">other</code>.

```csharp
public bool Equals(GaugeStyle other)
```

#### Parameters

`other` [GaugeStyle](Jumbee.Console.GaugeStyle.md)

#### Returns

 bool

### <a id="Jumbee_Console_GaugeStyle_Equals_System_Object_"></a> Equals\(object?\)

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

### <a id="Jumbee_Console_GaugeStyle_GetHashCode"></a> GetHashCode\(\)

Returns the hash code for this instance.

```csharp
public override int GetHashCode()
```

#### Returns

 int

A 32-bit signed integer that is the hash code for this instance.

### <a id="Jumbee_Console_GaugeStyle_WithFill_Jumbee_Console_Color_"></a> WithFill\(Color\)

A copy with the fill recoloured (keeps the track and text).

```csharp
public GaugeStyle WithFill(Color fill)
```

#### Parameters

`fill` [Color](Jumbee.Console.Color.md)

#### Returns

 [GaugeStyle](Jumbee.Console.GaugeStyle.md)

## Operators

### <a id="Jumbee_Console_GaugeStyle_op_Equality_Jumbee_Console_GaugeStyle_Jumbee_Console_GaugeStyle_"></a> operator ==\(GaugeStyle, GaugeStyle\)

Equality operator.

```csharp
public static bool operator ==(GaugeStyle a, GaugeStyle b)
```

#### Parameters

`a` [GaugeStyle](Jumbee.Console.GaugeStyle.md)

`b` [GaugeStyle](Jumbee.Console.GaugeStyle.md)

#### Returns

 bool

### <a id="Jumbee_Console_GaugeStyle_op_Inequality_Jumbee_Console_GaugeStyle_Jumbee_Console_GaugeStyle_"></a> operator \!=\(GaugeStyle, GaugeStyle\)

Inequality operator.

```csharp
public static bool operator !=(GaugeStyle a, GaugeStyle b)
```

#### Parameters

`a` [GaugeStyle](Jumbee.Console.GaugeStyle.md)

`b` [GaugeStyle](Jumbee.Console.GaugeStyle.md)

#### Returns

 bool

