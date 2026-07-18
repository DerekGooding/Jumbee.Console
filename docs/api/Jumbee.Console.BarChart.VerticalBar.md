# <a id="Jumbee_Console_BarChart_VerticalBar"></a> Class BarChart.VerticalBar

Namespace: [Jumbee.Console](Jumbee.Console.md)  
Assembly: Jumbee.Console.dll  

A vertically-drawn bar in a <xref href="Jumbee.Console.BarChart" data-throw-if-not-resolved="false"></xref>.

```csharp
protected class BarChart.VerticalBar : Renderable, BarChart.IBarControl
```

#### Inheritance

object ← 
Renderable ← 
[BarChart.VerticalBar](Jumbee.Console.BarChart.VerticalBar.md)

#### Implements

[BarChart.IBarControl](Jumbee.Console.BarChart.IBarControl.md)

## Properties

### <a id="Jumbee_Console_BarChart_VerticalBar_AsciiBar"></a> AsciiBar

The glyph used to draw the bar in ASCII mode.

```csharp
public char AsciiBar { get; set; }
```

#### Property Value

 char

### <a id="Jumbee_Console_BarChart_VerticalBar_Color"></a> Color

The bar's colour.

```csharp
public Color Color { get; set; }
```

#### Property Value

 [Color](Jumbee.Console.Color.md)

### <a id="Jumbee_Console_BarChart_VerticalBar_Height"></a> Height

The bar's height in rows.

```csharp
public int Height { get; set; }
```

#### Property Value

 int

### <a id="Jumbee_Console_BarChart_VerticalBar_MaxValue"></a> MaxValue

The value corresponding to a full-height bar.

```csharp
public double MaxValue { get; set; }
```

#### Property Value

 double

### <a id="Jumbee_Console_BarChart_VerticalBar_UnicodeBar"></a> UnicodeBar

The glyph used to draw the bar in Unicode mode.

```csharp
public char UnicodeBar { get; set; }
```

#### Property Value

 char

### <a id="Jumbee_Console_BarChart_VerticalBar_Value"></a> Value

The bar's value.

```csharp
public double Value { get; set; }
```

#### Property Value

 double

## Methods

### <a id="Jumbee_Console_BarChart_VerticalBar_Measure_Spectre_Console_Rendering_RenderOptions_System_Int32_"></a> Measure\(RenderOptions, int\)

Measures the renderable object.

```csharp
protected override Measurement Measure(RenderOptions options, int maxWidth)
```

#### Parameters

`options` RenderOptions

The render options.

`maxWidth` int

The maximum allowed width.

#### Returns

 Measurement

The minimum and maximum width of the object.

### <a id="Jumbee_Console_BarChart_VerticalBar_Render_Spectre_Console_Rendering_RenderOptions_System_Int32_"></a> Render\(RenderOptions, int\)

Renders the object.

```csharp
protected override IEnumerable<Segment> Render(RenderOptions options, int maxWidth)
```

#### Parameters

`options` RenderOptions

The render options.

`maxWidth` int

The maximum allowed width.

#### Returns

 IEnumerable<Segment\>

A collection of segments.

