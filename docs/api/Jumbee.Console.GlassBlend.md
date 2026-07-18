# <a id="Jumbee_Console_GlassBlend"></a> Class GlassBlend

Namespace: [Jumbee.Console](Jumbee.Console.md)  
Assembly: Jumbee.Console.dll  

Colour blending for <xref href="Jumbee.Console.GlassPanel" data-throw-if-not-resolved="false"></xref>: a gamma-space lerp (cheap, matches <xref href="ConsoleGUI.Data.Color.Mix(ConsoleGUI.Data.Color%40%2cSystem.Single)" data-throw-if-not-resolved="false"></xref>) or a
gamma-correct blend in linear light via two lookup tables (no runtime <code>pow</code>), plus a rough estimate of how
much of a cell a glyph inks (for the perceived-colour collapse).

```csharp
public static class GlassBlend
```

#### Inheritance

object ← 
[GlassBlend](Jumbee.Console.GlassBlend.md)

## Methods

### <a id="Jumbee_Console_GlassBlend_Blend_ConsoleGUI_Data_Color__ConsoleGUI_Data_Color__System_Single_System_Boolean_"></a> Blend\(in Color, in Color, float, bool\)

Blends <code class="paramref">from</code> toward <code class="paramref">to</code> by <code class="paramref">factor</code> (0..1),
    in gamma space, or in linear light when <code class="paramref">gammaCorrect</code> is set.

```csharp
public static Color Blend(in Color from, in Color to, float factor, bool gammaCorrect)
```

#### Parameters

`from` Color

`to` Color

`factor` float

`gammaCorrect` bool

#### Returns

 Color

### <a id="Jumbee_Console_GlassBlend_EstimateCoverage_System_Char_"></a> EstimateCoverage\(char\)

A rough fraction (0..1) of a cell inked by <code class="paramref">c</code>: blocks/shades by their fill, a
    space by nothing, ordinary text by a representative ink ratio. Used to collapse a cell to one colour.

```csharp
public static float EstimateCoverage(char c)
```

#### Parameters

`c` char

#### Returns

 float

