# <a id="Jumbee_Console_Drawing_WorldMap"></a> Class WorldMap

Namespace: [Jumbee.Console.Drawing](Jumbee.Console.Drawing.md)  
Assembly: Jumbee.Console.dll  

A world map: the Earth's coastlines as a cloud of longitude/latitude points (EPSG:4326), for drawing on a
<xref href="Jumbee.Console.Canvas" data-throw-if-not-resolved="false"></xref> whose bounds are set to <code>X [-180, 180]</code> and <code>Y [-90, 90]</code> (a
sub-region zooms in).

```csharp
public sealed class WorldMap : IShape
```

#### Inheritance

object ← 
[WorldMap](Jumbee.Console.Drawing.WorldMap.md)

#### Implements

[IShape](Jumbee.Console.Drawing.IShape.md)

## Remarks

Ported from Ratatui's <code>Map</code>; the point data is Natural Earth coastline data.

## Constructors

### <a id="Jumbee_Console_Drawing_WorldMap__ctor_Jumbee_Console_Color_Jumbee_Console_Drawing_MapResolution_"></a> WorldMap\(Color, MapResolution\)

```csharp
public WorldMap(Color color, MapResolution resolution = MapResolution.Low)
```

#### Parameters

`color` [Color](Jumbee.Console.Color.md)

Colour of the map points.

`resolution` [MapResolution](Jumbee.Console.Drawing.MapResolution.md)

Point density (see <xref href="Jumbee.Console.Drawing.MapResolution" data-throw-if-not-resolved="false"></xref>). Default <xref href="Jumbee.Console.Drawing.MapResolution.Low" data-throw-if-not-resolved="false"></xref>.

## Properties

### <a id="Jumbee_Console_Drawing_WorldMap_Color"></a> Color

Colour of the map points.

```csharp
public Color Color { get; }
```

#### Property Value

 [Color](Jumbee.Console.Color.md)

### <a id="Jumbee_Console_Drawing_WorldMap_Resolution"></a> Resolution

Point density of the coastline sampling.

```csharp
public MapResolution Resolution { get; }
```

#### Property Value

 [MapResolution](Jumbee.Console.Drawing.MapResolution.md)

