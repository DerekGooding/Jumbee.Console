# <a id="Jumbee_Console_PlotBrush"></a> Enum PlotBrush

Namespace: [Jumbee.Console](Jumbee.Console.md)  
Assembly: Jumbee.Console.dll  

Selects the glyph set (and thus the sub-cell resolution) a <xref href="Jumbee.Console.Plot" data-throw-if-not-resolved="false"></xref> series is drawn with.

```csharp
public enum PlotBrush
```

## Fields

`Braille = 0` 

Braille dots — 2×4 sub-cells per character (8 points/cell), the smoothest. The default.



`Quadrant = 1` 

Quadrant blocks — 2×2 sub-cells per character (4 points/cell), solid blocks rather than dots.



`Block = 2` 

A solid full block <code>█</code> per point (1×1).



`Dot = 3` 

A <code>•</code> per point (1×1).



`Star = 4` 

A <code>*</code> per point (1×1).



## Remarks

Higher resolution packs more plotted points into each character cell for a smoother line.

