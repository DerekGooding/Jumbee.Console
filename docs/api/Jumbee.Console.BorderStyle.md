# <a id="Jumbee_Console_BorderStyle"></a> Enum BorderStyle

Namespace: [Jumbee.Console](Jumbee.Console.md)  
Assembly: Jumbee.Console.Styles.dll  

The shape of a control frame's border.

```csharp
public enum BorderStyle
```

## Fields

`None = 0` 

No border is drawn.



`Ascii = 1` 

An ASCII border using <code>+</code>, <code>-</code>, and <code>|</code> characters.



`Double = 2` 

A double-line box-drawing border.



`Heavy = 3` 

A heavy (thick) line box-drawing border.



`Rounded = 4` 

A single-line border with rounded corners.



`Square = 5` 

A single-line border with square corners.



## Remarks

Each value selects a box-drawing glyph set; <xref href="Jumbee.Console.BorderStyle.None" data-throw-if-not-resolved="false"></xref> draws no border. A frame's default
    border comes from <xref href="Jumbee.Console.IStyleTheme.FrameBorder" data-throw-if-not-resolved="false"></xref>.

