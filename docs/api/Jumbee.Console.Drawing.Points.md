# <a id="Jumbee_Console_Drawing_Points"></a> Class Points

Namespace: [Jumbee.Console.Drawing](Jumbee.Console.Drawing.md)  
Assembly: Jumbee.Console.dll  

A scatter of individual points in canvas coordinates.

```csharp
public sealed class Points : IShape
```

#### Inheritance

object ← 
[Points](Jumbee.Console.Drawing.Points.md)

#### Implements

[IShape](Jumbee.Console.Drawing.IShape.md)

## Constructors

### <a id="Jumbee_Console_Drawing_Points__ctor_System_Collections_Generic_IReadOnlyList_System_ValueTuple_System_Double_System_Double___Jumbee_Console_Color_"></a> Points\(IReadOnlyList<\(double X, double Y\)\>, Color\)

Initializes a new <xref href="Jumbee.Console.Drawing.Points" data-throw-if-not-resolved="false"></xref> scatter from the given coordinates and colour.

```csharp
public Points(IReadOnlyList<(double X, double Y)> coords, Color color)
```

#### Parameters

`coords` IReadOnlyList<\(double X, double Y\)\>

`color` [Color](Jumbee.Console.Color.md)

## Properties

### <a id="Jumbee_Console_Drawing_Points_Color"></a> Color

Colour of the points.

```csharp
public Color Color { get; }
```

#### Property Value

 [Color](Jumbee.Console.Color.md)

### <a id="Jumbee_Console_Drawing_Points_Coords"></a> Coords

The point coordinates in canvas space.

```csharp
public IReadOnlyList<(double X, double Y)> Coords { get; }
```

#### Property Value

 IReadOnlyList<\(double X, double Y\)\>

