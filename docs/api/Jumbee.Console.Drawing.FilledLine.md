# <a id="Jumbee_Console_Drawing_FilledLine"></a> Class FilledLine

Namespace: [Jumbee.Console.Drawing](Jumbee.Console.Drawing.md)  
Assembly: Jumbee.Console.dll  

A line whose area between the line and <xref href="Jumbee.Console.Drawing.FilledLine.FillToY" data-throw-if-not-resolved="false"></xref> is filled — useful for area charts.

```csharp
public sealed class FilledLine : IShape
```

#### Inheritance

object ← 
[FilledLine](Jumbee.Console.Drawing.FilledLine.md)

#### Implements

[IShape](Jumbee.Console.Drawing.IShape.md)

## Remarks

The fill runs vertically from each line point to the row of <xref href="Jumbee.Console.Drawing.FilledLine.FillToY" data-throw-if-not-resolved="false"></xref>.

## Constructors

### <a id="Jumbee_Console_Drawing_FilledLine__ctor_System_Double_System_Double_System_Double_System_Double_System_Double_Jumbee_Console_Color_"></a> FilledLine\(double, double, double, double, double, Color\)

Initializes a new <xref href="Jumbee.Console.Drawing.FilledLine" data-throw-if-not-resolved="false"></xref> from (<code class="paramref">x1</code>, <code class="paramref">y1</code>) to (<code class="paramref">x2</code>, <code class="paramref">y2</code>), filled down to <code class="paramref">fillToY</code>, in the given colour.

```csharp
public FilledLine(double x1, double y1, double x2, double y2, double fillToY, Color color)
```

#### Parameters

`x1` double

`y1` double

`x2` double

`y2` double

`fillToY` double

`color` [Color](Jumbee.Console.Color.md)

## Properties

### <a id="Jumbee_Console_Drawing_FilledLine_Color"></a> Color

Colour of the line and its fill.

```csharp
public Color Color { get; }
```

#### Property Value

 [Color](Jumbee.Console.Color.md)

### <a id="Jumbee_Console_Drawing_FilledLine_FillToY"></a> FillToY

Y coordinate the fill extends to from each line point.

```csharp
public double FillToY { get; }
```

#### Property Value

 double

### <a id="Jumbee_Console_Drawing_FilledLine_X1"></a> X1

X coordinate of the start point.

```csharp
public double X1 { get; }
```

#### Property Value

 double

### <a id="Jumbee_Console_Drawing_FilledLine_X2"></a> X2

X coordinate of the end point.

```csharp
public double X2 { get; }
```

#### Property Value

 double

### <a id="Jumbee_Console_Drawing_FilledLine_Y1"></a> Y1

Y coordinate of the start point.

```csharp
public double Y1 { get; }
```

#### Property Value

 double

### <a id="Jumbee_Console_Drawing_FilledLine_Y2"></a> Y2

Y coordinate of the end point.

```csharp
public double Y2 { get; }
```

#### Property Value

 double

