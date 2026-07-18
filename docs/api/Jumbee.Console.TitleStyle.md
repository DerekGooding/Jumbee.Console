# <a id="Jumbee_Console_TitleStyle"></a> Struct TitleStyle

Namespace: [Jumbee.Console](Jumbee.Console.md)  
Assembly: Jumbee.Console.Styles.dll  

Describes how a control frame title is aligned, bordered, and colored.

```csharp
public readonly struct TitleStyle
```

## Remarks

A frame's default title style comes from <xref href="Jumbee.Console.IStyleTheme.TitleStyle" data-throw-if-not-resolved="false"></xref>.

## Constructors

### <a id="Jumbee_Console_TitleStyle__ctor_Jumbee_Console_TitlePos_Jumbee_Console_TitleBorderStyle_Jumbee_Console_TitleColorStyle_"></a> TitleStyle\(TitlePos, TitleBorderStyle, TitleColorStyle\)

Initializes a new <xref href="Jumbee.Console.TitleStyle" data-throw-if-not-resolved="false"></xref> with the given position, border style, and color style.

```csharp
public TitleStyle(TitlePos pos = TitlePos.TopLeft, TitleBorderStyle borderStyle = TitleBorderStyle.Double, TitleColorStyle color = TitleColorStyle.Normal)
```

#### Parameters

`pos` [TitlePos](Jumbee.Console.TitlePos.md)

`borderStyle` [TitleBorderStyle](Jumbee.Console.TitleBorderStyle.md)

`color` [TitleColorStyle](Jumbee.Console.TitleColorStyle.md)

## Properties

### <a id="Jumbee_Console_TitleStyle_BorderStyle"></a> BorderStyle

How the title is drawn relative to the top border.

```csharp
public TitleBorderStyle BorderStyle { get; init; }
```

#### Property Value

 [TitleBorderStyle](Jumbee.Console.TitleBorderStyle.md)

### <a id="Jumbee_Console_TitleStyle_Color"></a> Color

How the title is colored relative to the border color.

```csharp
public TitleColorStyle Color { get; init; }
```

#### Property Value

 [TitleColorStyle](Jumbee.Console.TitleColorStyle.md)

### <a id="Jumbee_Console_TitleStyle_Default"></a> Default

The default title style (top-left, double border, normal colors), matching the original behavior.

```csharp
public static TitleStyle Default { get; }
```

#### Property Value

 [TitleStyle](Jumbee.Console.TitleStyle.md)

### <a id="Jumbee_Console_TitleStyle_Pos"></a> Pos

The title's border and alignment position.

```csharp
public TitlePos Pos { get; init; }
```

#### Property Value

 [TitlePos](Jumbee.Console.TitlePos.md)

## Methods

### <a id="Jumbee_Console_TitleStyle_Equals_Jumbee_Console_TitleStyle_"></a> Equals\(TitleStyle\)

Determines whether this <xref href="Jumbee.Console.TitleStyle" data-throw-if-not-resolved="false"></xref> equals <code class="paramref">other</code>.

```csharp
public bool Equals(TitleStyle other)
```

#### Parameters

`other` [TitleStyle](Jumbee.Console.TitleStyle.md)

#### Returns

 bool

### <a id="Jumbee_Console_TitleStyle_Equals_System_Object_"></a> Equals\(object?\)

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

### <a id="Jumbee_Console_TitleStyle_GetHashCode"></a> GetHashCode\(\)

Returns the hash code for this instance.

```csharp
public override int GetHashCode()
```

#### Returns

 int

A 32-bit signed integer that is the hash code for this instance.

## Operators

### <a id="Jumbee_Console_TitleStyle_op_Equality_Jumbee_Console_TitleStyle_Jumbee_Console_TitleStyle_"></a> operator ==\(TitleStyle, TitleStyle\)

Equality operator.

```csharp
public static bool operator ==(TitleStyle a, TitleStyle b)
```

#### Parameters

`a` [TitleStyle](Jumbee.Console.TitleStyle.md)

`b` [TitleStyle](Jumbee.Console.TitleStyle.md)

#### Returns

 bool

### <a id="Jumbee_Console_TitleStyle_op_Inequality_Jumbee_Console_TitleStyle_Jumbee_Console_TitleStyle_"></a> operator \!=\(TitleStyle, TitleStyle\)

Inequality operator.

```csharp
public static bool operator !=(TitleStyle a, TitleStyle b)
```

#### Parameters

`a` [TitleStyle](Jumbee.Console.TitleStyle.md)

`b` [TitleStyle](Jumbee.Console.TitleStyle.md)

#### Returns

 bool

