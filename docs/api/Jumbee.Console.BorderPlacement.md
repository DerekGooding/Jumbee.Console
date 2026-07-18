# <a id="Jumbee_Console_BorderPlacement"></a> Enum BorderPlacement

Namespace: [Jumbee.Console](Jumbee.Console.md)  
Assembly: Jumbee.Console.Styles.dll  

Which edges of a control frame's border are drawn.

```csharp
[Flags]
public enum BorderPlacement
```

## Fields

`None = 0` 

No edges — the border shape draws nothing (like <xref href="Jumbee.Console.BorderStyle.None" data-throw-if-not-resolved="false"></xref>).



`Left = 1` 

The left edge.



`Top = 2` 

The top edge.



`Right = 4` 

The right edge.



`Bottom = 8` 

The bottom edge.



`All = 15` 

All four edges (the default).



## Remarks

A <xref href="System.FlagsAttribute" data-throw-if-not-resolved="false"></xref> set — combine sides with <code>|</code> (e.g. <code>Top | Bottom</code> for
    horizontal rules only). Defaults to <xref href="Jumbee.Console.BorderPlacement.All" data-throw-if-not-resolved="false"></xref>. Set via <code>ControlFrame.BorderPlacement</code>.

