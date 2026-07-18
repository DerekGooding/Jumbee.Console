# <a id="Jumbee_Console_FocusStyle"></a> Enum FocusStyle

Namespace: [Jumbee.Console](Jumbee.Console.md)  
Assembly: Jumbee.Console.Styles.dll  

How the themed default focus cue is drawn on a focused control that isn't showing focus another way (no visible
frame border, and <code>Control.RendersOwnFocus</code> is false).

```csharp
public enum FocusStyle
```

## Fields

`Tint = 0` 

Tint the whole control with the focus background — a solid, obvious cue (the default).



`Ring = 1` 

Tint only the control's outer edge cells with the focus background — a subtler focus ring.



`Underline = 2` 

Underline the control's bottom row — a minimal cue that leaves the content colours untouched.



## Remarks

The colour comes from <xref href="Jumbee.Console.IStyleTheme.Focus" data-throw-if-not-resolved="false"></xref>; the mode from
    <xref href="Jumbee.Console.IStyleTheme.FocusStyle" data-throw-if-not-resolved="false"></xref>.

