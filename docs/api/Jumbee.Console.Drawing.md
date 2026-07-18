# <a id="Jumbee_Console_Drawing"></a> Namespace Jumbee.Console.Drawing

### Classes

 [Circle](Jumbee.Console.Drawing.Circle.md)

A circle outline traced at one-degree steps around its centre, in canvas coordinates.

 [FilledLine](Jumbee.Console.Drawing.FilledLine.md)

A line whose area between the line and <xref href="Jumbee.Console.Drawing.FilledLine.FillToY" data-throw-if-not-resolved="false"></xref> is filled — useful for area charts.

 [Line](Jumbee.Console.Drawing.Line.md)

A straight line between two points in canvas coordinates.

 [Points](Jumbee.Console.Drawing.Points.md)

A scatter of individual points in canvas coordinates.

 [Rectangle](Jumbee.Console.Drawing.Rectangle.md)

A rectangle outline. Positioned from its bottom-left corner (<xref href="Jumbee.Console.Drawing.Rectangle.X" data-throw-if-not-resolved="false"></xref>, <xref href="Jumbee.Console.Drawing.Rectangle.Y" data-throw-if-not-resolved="false"></xref>) in canvas
coordinates — the mathematical convention, not terminal cells.

 [WorldMap](Jumbee.Console.Drawing.WorldMap.md)

A world map: the Earth's coastlines as a cloud of longitude/latitude points (EPSG:4326), for drawing on a
<xref href="Jumbee.Console.Canvas" data-throw-if-not-resolved="false"></xref> whose bounds are set to <code>X [-180, 180]</code> and <code>Y [-90, 90]</code> (a
sub-region zooms in).

### Interfaces

 [IShape](Jumbee.Console.Drawing.IShape.md)

A shape that can be drawn on a <xref href="Jumbee.Console.Canvas" data-throw-if-not-resolved="false"></xref> via <xref href="Jumbee.Console.Canvas.Add(Jumbee.Console.Drawing.IShape)" data-throw-if-not-resolved="false"></xref>.

### Enums

 [CanvasMarker](Jumbee.Console.Drawing.CanvasMarker.md)

Selects the glyph set (and thus the sub-cell resolution) a <xref href="Jumbee.Console.Canvas" data-throw-if-not-resolved="false"></xref> draws its shapes
with. The default is <xref href="Jumbee.Console.Drawing.CanvasMarker.Braille" data-throw-if-not-resolved="false"></xref>.

 [MapResolution](Jumbee.Console.Drawing.MapResolution.md)

The point density of a <xref href="Jumbee.Console.Drawing.WorldMap" data-throw-if-not-resolved="false"></xref> — how finely the coastlines are sampled.

