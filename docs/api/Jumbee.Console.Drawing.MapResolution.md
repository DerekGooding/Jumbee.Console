# <a id="Jumbee_Console_Drawing_MapResolution"></a> Enum MapResolution

Namespace: [Jumbee.Console.Drawing](Jumbee.Console.Drawing.md)  
Assembly: Jumbee.Console.dll  

The point density of a <xref href="Jumbee.Console.Drawing.WorldMap" data-throw-if-not-resolved="false"></xref> — how finely the coastlines are sampled.

```csharp
public enum MapResolution
```

## Fields

`Low = 0` 

~1166 points — coarse; fine with a 1×1 marker such as <xref href="Jumbee.Console.Drawing.CanvasMarker.Dot" data-throw-if-not-resolved="false"></xref>. The default.



`High = 1` 

~5125 points — detailed; best drawn with <xref href="Jumbee.Console.Drawing.CanvasMarker.Braille" data-throw-if-not-resolved="false"></xref> for a crisp coastline.



