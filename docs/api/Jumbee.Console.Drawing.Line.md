# <a id="Jumbee_Console_Drawing_Line"></a> Class Line

Namespace: [Jumbee.Console.Drawing](Jumbee.Console.Drawing.md)  
Assembly: Jumbee.Console.dll  

A straight line between two points in canvas coordinates.

```csharp
public sealed class Line : IShape
```

#### Inheritance

object ← 
[Line](Jumbee.Console.Drawing.Line.md)

#### Implements

[IShape](Jumbee.Console.Drawing.IShape.md)

## Constructors

### <a id="Jumbee_Console_Drawing_Line__ctor_System_Double_System_Double_System_Double_System_Double_Jumbee_Console_Color_"></a> Line\(double, double, double, double, Color\)

Initializes a new <xref href="Jumbee.Console.Drawing.Line" data-throw-if-not-resolved="false"></xref> from (<code class="paramref">x1</code>, <code class="paramref">y1</code>) to (<code class="paramref">x2</code>, <code class="paramref">y2</code>) in the given colour.

```csharp
public Line(double x1, double y1, double x2, double y2, Color color)
```

#### Parameters

`x1` double

`y1` double

`x2` double

`y2` double

`color` [Color](Jumbee.Console.Color.md)

## Properties

### <a id="Jumbee_Console_Drawing_Line_Color"></a> Color

Colour of the line.

```csharp
public Color Color { get; }
```

#### Property Value

 [Color](Jumbee.Console.Color.md)

### <a id="Jumbee_Console_Drawing_Line_X1"></a> X1

X coordinate of the start point.

```csharp
public double X1 { get; }
```

#### Property Value

 double

### <a id="Jumbee_Console_Drawing_Line_X2"></a> X2

X coordinate of the end point.

```csharp
public double X2 { get; }
```

#### Property Value

 double

### <a id="Jumbee_Console_Drawing_Line_Y1"></a> Y1

Y coordinate of the start point.

```csharp
public double Y1 { get; }
```

#### Property Value

 double

### <a id="Jumbee_Console_Drawing_Line_Y2"></a> Y2

Y coordinate of the end point.

```csharp
public double Y2 { get; }
```

#### Property Value

 double

