# <a id="Jumbee_Console_ScrollBarMode"></a> Enum ScrollBarMode

Namespace: [Jumbee.Console](Jumbee.Console.md)  
Assembly: Jumbee.Console.Styles.dll  

How a control frame renders its vertical scrollbar.

```csharp
public enum ScrollBarMode
```

## Fields

`Smooth = 0` 

Modern-style bar: a solid thumb over a solid track whose ends render at <em>sub-cell</em>
    resolution using eighth-block glyphs (<code>▁▂▃▄▅▆▇█</code>) so the thumb glides smoothly rather than snapping to
    whole rows.

No end arrows; the thumb spans the whole column. Assumes a terminal with block-glyph support
    (Windows Terminal and most modern emulators). The thumb/track <em>glyphs</em> in this struct are ignored in
    this mode (the bar computes its own); only the <xref href="Jumbee.Console.ScrollBarStyle" data-throw-if-not-resolved="false"></xref> colours are used.

`Classic = 1` 

The classic three-part bar: the fixed <xref href="Jumbee.Console.ScrollBarGlyphs.Thumb" data-throw-if-not-resolved="false"></xref>/<xref href="Jumbee.Console.ScrollBarGlyphs.Track" data-throw-if-not-resolved="false"></xref>
    glyphs and <xref href="Jumbee.Console.ScrollBarGlyphs.UpArrow" data-throw-if-not-resolved="false"></xref>/<xref href="Jumbee.Console.ScrollBarGlyphs.DownArrow" data-throw-if-not-resolved="false"></xref> end arrows, positioned
    in whole cells. Portable to legacy terminals that lack eighth-block glyphs.



