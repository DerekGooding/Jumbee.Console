# <a id="Jumbee_Console_Drawing_IShape"></a> Interface IShape

Namespace: [Jumbee.Console.Drawing](Jumbee.Console.Drawing.md)  
Assembly: Jumbee.Console.dll  

A shape that can be drawn on a <xref href="Jumbee.Console.Canvas" data-throw-if-not-resolved="false"></xref> via <xref href="Jumbee.Console.Canvas.Add(Jumbee.Console.Drawing.IShape)" data-throw-if-not-resolved="false"></xref>.

```csharp
public interface IShape
```

## Remarks

The built-in shapes (<xref href="Jumbee.Console.Drawing.Line" data-throw-if-not-resolved="false"></xref>, <xref href="Jumbee.Console.Drawing.FilledLine" data-throw-if-not-resolved="false"></xref>, <xref href="Jumbee.Console.Drawing.Rectangle" data-throw-if-not-resolved="false"></xref>, <xref href="Jumbee.Console.Drawing.Points" data-throw-if-not-resolved="false"></xref>,
<xref href="Jumbee.Console.Drawing.Circle" data-throw-if-not-resolved="false"></xref>) implement it; drawing goes through an internal painter, so this is a closed set.

