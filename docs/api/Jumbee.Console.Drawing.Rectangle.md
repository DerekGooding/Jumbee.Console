# <a id="Jumbee_Console_Drawing_Rectangle"></a> Class Rectangle

Namespace: [Jumbee.Console.Drawing](Jumbee.Console.Drawing.md)  
Assembly: Jumbee.Console.dll  

A rectangle outline. Positioned from its bottom-left corner (<xref href="Jumbee.Console.Drawing.Rectangle.X" data-throw-if-not-resolved="false"></xref>, <xref href="Jumbee.Console.Drawing.Rectangle.Y" data-throw-if-not-resolved="false"></xref>) in canvas
coordinates — the mathematical convention, not terminal cells.

```csharp
public sealed class Rectangle : IShape
```

#### Inheritance

object ← 
[Rectangle](Jumbee.Console.Drawing.Rectangle.md)

#### Implements

[IShape](Jumbee.Console.Drawing.IShape.md)

## Constructors

### <a id="Jumbee_Console_Drawing_Rectangle__ctor_System_Double_System_Double_System_Double_System_Double_Jumbee_Console_Color_"></a> Rectangle\(double, double, double, double, Color\)

Initializes a new <xref href="Jumbee.Console.Drawing.Rectangle" data-throw-if-not-resolved="false"></xref> at bottom-left corner (<code class="paramref">x</code>, <code class="paramref">y</code>) with the given size and colour.

```csharp
public Rectangle(double x, double y, double width, double height, Color color)
```

#### Parameters

`x` double

`y` double

`width` double

`height` double

`color` [Color](Jumbee.Console.Color.md)

## Properties

### <a id="Jumbee_Console_Drawing_Rectangle_Color"></a> Color

Colour of the outline.

```csharp
public Color Color { get; }
```

#### Property Value

 [Color](Jumbee.Console.Color.md)

### <a id="Jumbee_Console_Drawing_Rectangle_Height"></a> Height

Height of the rectangle.

```csharp
public double Height { get; }
```

#### Property Value

 double

### <a id="Jumbee_Console_Drawing_Rectangle_Width"></a> Width

Width of the rectangle.

```csharp
public double Width { get; }
```

#### Property Value

 double

### <a id="Jumbee_Console_Drawing_Rectangle_X"></a> X

X coordinate of the bottom-left corner.

```csharp
public double X { get; }
```

#### Property Value

 double

### <a id="Jumbee_Console_Drawing_Rectangle_Y"></a> Y

Y coordinate of the bottom-left corner.

```csharp
public double Y { get; }
```

#### Property Value

 double

