# <a id="Jumbee_Console_TreeGuide"></a> Enum TreeGuide

Namespace: [Jumbee.Console](Jumbee.Console.md)  
Assembly: Jumbee.Console.dll  

The line style used to draw the connecting guide lines of a <xref href="Jumbee.Console.Tree" data-throw-if-not-resolved="false"></xref>.

```csharp
public enum TreeGuide
```

## Fields

`Ascii = 0` 

ASCII guide lines (<code>|</code>, <code>+</code>, <code>-</code>) for terminals without Unicode support.



`Line = 1` 

Single box-drawing guide lines.



`BoldLine = 2` 

Heavy (bold) box-drawing guide lines.



`DoubleLine = 3` 

Double box-drawing guide lines.



`None = 4` 

No connector lines — hierarchy is shown by indentation (and any node glyphs) alone. Useful for a
    clean, icon-driven tree.



