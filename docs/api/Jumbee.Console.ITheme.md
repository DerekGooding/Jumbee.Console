# <a id="Jumbee_Console_ITheme"></a> Interface ITheme

Namespace: [Jumbee.Console](Jumbee.Console.md)  
Assembly: Jumbee.Console.Styles.dll  

A complete theme bundling an <xref href="Jumbee.Console.IStyleTheme" data-throw-if-not-resolved="false"></xref> and an <xref href="Jumbee.Console.IGlyphTheme" data-throw-if-not-resolved="false"></xref>, for callers that want
to customise both colours/styles and glyphs as one unit and apply them together (via <code>UI.SetTheme(ITheme)</code>).

```csharp
public interface ITheme
```

## Remarks

Both halves are default-implemented, so a theme may supply only the side it cares about.

## Properties

### <a id="Jumbee_Console_ITheme_Glyphs"></a> Glyphs

The glyph half. Defaults to <xref href="Jumbee.Console.DefaultGlyphTheme" data-throw-if-not-resolved="false"></xref>.

```csharp
IGlyphTheme Glyphs { get; }
```

#### Property Value

 [IGlyphTheme](Jumbee.Console.IGlyphTheme.md)

### <a id="Jumbee_Console_ITheme_Styles"></a> Styles

The style half (colours/decoration plus the non-glyph selectors). Defaults to <xref href="Jumbee.Console.DefaultStyleTheme" data-throw-if-not-resolved="false"></xref>.

```csharp
IStyleTheme Styles { get; }
```

#### Property Value

 [IStyleTheme](Jumbee.Console.IStyleTheme.md)

