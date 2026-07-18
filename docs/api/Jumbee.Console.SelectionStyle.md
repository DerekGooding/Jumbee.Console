# <a id="Jumbee_Console_SelectionStyle"></a> Enum SelectionStyle

Namespace: [Jumbee.Console](Jumbee.Console.md)  
Assembly: Jumbee.Console.Styles.dll  

How a navigable control (e.g. <xref href="Jumbee.Console.IStyleTheme" data-throw-if-not-resolved="false"></xref> consumers like ListBox, Tree, TabPanel) renders its
selected/active item.

```csharp
public enum SelectionStyle
```

#### Extension Methods

[SelectionStylesExtensions.Prefix\(SelectionStyle, string\)](Jumbee.Console.SelectionStylesExtensions.md\#Jumbee\_Console\_SelectionStylesExtensions\_Prefix\_Jumbee\_Console\_SelectionStyle\_System\_String\_), 
[SelectionStylesExtensions.TextStyle\(SelectionStyle, Color?, Color?\)](Jumbee.Console.SelectionStylesExtensions.md\#Jumbee\_Console\_SelectionStylesExtensions\_TextStyle\_Jumbee\_Console\_SelectionStyle\_System\_Nullable\_Jumbee\_Console\_Color\_\_System\_Nullable\_Jumbee\_Console\_Color\_\_)

## Fields

`Highlight = 0` 

Paint the selected item with the selection foreground <em>and</em> background.



`Underline = 1` 

Underline the selected item (selection foreground, no background).



`Caret = 2` 

Prefix the selected item with the selection caret glyph (selection foreground, no background).



## Remarks

The default comes from <xref href="Jumbee.Console.IStyleTheme.SelectionStyle" data-throw-if-not-resolved="false"></xref>; the caret glyph used by
    <xref href="Jumbee.Console.SelectionStyle.Caret" data-throw-if-not-resolved="false"></xref> comes from <xref href="Jumbee.Console.IGlyphTheme.SelectionCaret" data-throw-if-not-resolved="false"></xref>.

