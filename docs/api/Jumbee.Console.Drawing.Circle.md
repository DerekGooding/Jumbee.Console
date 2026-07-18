# <a id="Jumbee_Console_Drawing_Circle"></a> Class Circle

Namespace: [Jumbee.Console.Drawing](Jumbee.Console.Drawing.md)  
Assembly: Jumbee.Console.dll  

A circle outline traced at one-degree steps around its centre, in canvas coordinates.

```csharp
public sealed class Circle : IShape
```

#### Inheritance

object ← 
[Circle](Jumbee.Console.Drawing.Circle.md)

#### Implements

[IShape](Jumbee.Console.Drawing.IShape.md)

## Constructors

### <a id="Jumbee_Console_Drawing_Circle__ctor_System_Double_System_Double_System_Double_Jumbee_Console_Color_"></a> Circle\(double, double, double, Color\)

Initializes a new <xref href="Jumbee.Console.Drawing.Circle" data-throw-if-not-resolved="false"></xref> centred at (<code class="paramref">x</code>, <code class="paramref">y</code>) with the given radius and colour.

```csharp
public Circle(double x, double y, double radius, Color color)
```

#### Parameters

`x` double

`y` double

`radius` double

`color` [Color](Jumbee.Console.Color.md)

## Properties

### <a id="Jumbee_Console_Drawing_Circle_Color"></a> Color

Colour of the outline.

```csharp
public Color Color { get; }
```

#### Property Value

 [Color](Jumbee.Console.Color.md)

### <a id="Jumbee_Console_Drawing_Circle_Radius"></a> Radius

Radius of the circle.

```csharp
public double Radius { get; }
```

#### Property Value

 double

### <a id="Jumbee_Console_Drawing_Circle_X"></a> X

X coordinate of the centre.

```csharp
public double X { get; }
```

#### Property Value

 double

### <a id="Jumbee_Console_Drawing_Circle_Y"></a> Y

Y coordinate of the centre.

```csharp
public double Y { get; }
```

#### Property Value

 double

