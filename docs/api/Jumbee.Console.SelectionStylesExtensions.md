# <a id="Jumbee_Console_SelectionStylesExtensions"></a> Class SelectionStylesExtensions

Namespace: [Jumbee.Console](Jumbee.Console.md)  
Assembly: Jumbee.Console.Styles.dll  

Turns a <xref href="Jumbee.Console.SelectionStyle" data-throw-if-not-resolved="false"></xref> into the prefix + text style a control applies to its selected item.

```csharp
public static class SelectionStylesExtensions
```

#### Inheritance

object ← 
[SelectionStylesExtensions](Jumbee.Console.SelectionStylesExtensions.md)

## Methods

### <a id="Jumbee_Console_SelectionStylesExtensions_Prefix_Jumbee_Console_SelectionStyle_System_String_"></a> Prefix\(SelectionStyle, string\)

The string to prepend to the selected item: the caret glyph for <xref href="Jumbee.Console.SelectionStyle.Caret" data-throw-if-not-resolved="false"></xref>,
    otherwise the empty string.

```csharp
public static string Prefix(this SelectionStyle style, string caret)
```

#### Parameters

`style` [SelectionStyle](Jumbee.Console.SelectionStyle.md)

`caret` string

#### Returns

 string

### <a id="Jumbee_Console_SelectionStylesExtensions_TextStyle_Jumbee_Console_SelectionStyle_System_Nullable_Jumbee_Console_Color__System_Nullable_Jumbee_Console_Color__"></a> TextStyle\(SelectionStyle, Color?, Color?\)

The text style for the selected item: Highlight uses foreground + background; Underline uses the
    foreground plus an underline; Caret uses the foreground only (the caret carries the indication).

```csharp
public static Style TextStyle(this SelectionStyle style, Color? foreground, Color? background)
```

#### Parameters

`style` [SelectionStyle](Jumbee.Console.SelectionStyle.md)

`foreground` [Color](Jumbee.Console.Color.md)?

`background` [Color](Jumbee.Console.Color.md)?

#### Returns

 Style

