# <a id="Jumbee_Console_CursorStyle"></a> Enum CursorStyle

Namespace: [Jumbee.Console](Jumbee.Console.md)  
Assembly: Jumbee.Console.Styles.dll  

Terminal cursor shapes/blink, mapping to DECSCUSR (CSI Ps SP q) values. <xref href="Jumbee.Console.CursorStyle.Default" data-throw-if-not-resolved="false"></xref> (0) leaves
the terminal's configured cursor.

```csharp
public enum CursorStyle
```

## Fields

`Default = 0` 

Leave the terminal's configured cursor unchanged (DECSCUSR 0).



`BlinkingBlock = 1` 

A blinking block cursor (DECSCUSR 1).



`SteadyBlock = 2` 

A steady block cursor (DECSCUSR 2).



`BlinkingUnderline = 3` 

A blinking underline cursor (DECSCUSR 3).



`SteadyUnderline = 4` 

A steady underline cursor (DECSCUSR 4).



`BlinkingBar = 5` 

A blinking vertical-bar cursor (DECSCUSR 5).



`SteadyBar = 6` 

A steady vertical-bar cursor (DECSCUSR 6).



