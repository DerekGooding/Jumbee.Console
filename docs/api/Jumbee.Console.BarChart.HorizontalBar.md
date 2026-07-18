# <a id="Jumbee_Console_BarChart_HorizontalBar"></a> Class BarChart.HorizontalBar

Namespace: [Jumbee.Console](Jumbee.Console.md)  
Assembly: Jumbee.Console.dll  

A horizontally-drawn bar in a <xref href="Jumbee.Console.BarChart" data-throw-if-not-resolved="false"></xref>, optionally showing its value and remaining track.

```csharp
protected sealed class BarChart.HorizontalBar : Renderable, BarChart.IBarControl
```

#### Inheritance

object ← 
Renderable ← 
[BarChart.HorizontalBar](Jumbee.Console.BarChart.HorizontalBar.md)

#### Implements

[BarChart.IBarControl](Jumbee.Console.BarChart.IBarControl.md)

## Properties

### <a id="Jumbee_Console_BarChart_HorizontalBar_AsciiBar"></a> AsciiBar

The glyph used to draw the bar in ASCII mode.

```csharp
public char AsciiBar { get; set; }
```

#### Property Value

 char

### <a id="Jumbee_Console_BarChart_HorizontalBar_Color"></a> Color

The bar's colour (sets both the completed and finished styles).

```csharp
public Color Color { get; set; }
```

#### Property Value

 [Color](Jumbee.Console.Color.md)

### <a id="Jumbee_Console_BarChart_HorizontalBar_CompletedStyle"></a> CompletedStyle

The style of the filled portion while the bar is incomplete. Defaults to yellow.

```csharp
public Style CompletedStyle { get; set; }
```

#### Property Value

 [Style](Jumbee.Console.Style.md)

### <a id="Jumbee_Console_BarChart_HorizontalBar_Culture"></a> Culture

The culture used to format the value, or <a href="https://learn.microsoft.com/dotnet/csharp/language-reference/keywords/null">null</a> for the invariant culture.

```csharp
public CultureInfo? Culture { get; set; }
```

#### Property Value

 CultureInfo?

### <a id="Jumbee_Console_BarChart_HorizontalBar_FinishedStyle"></a> FinishedStyle

The style of the filled portion once the bar is complete. Defaults to green.

```csharp
public Style FinishedStyle { get; set; }
```

#### Property Value

 [Style](Jumbee.Console.Style.md)

### <a id="Jumbee_Console_BarChart_HorizontalBar_MaxValue"></a> MaxValue

The value corresponding to a full-width bar. Defaults to 100.

```csharp
public double MaxValue { get; set; }
```

#### Property Value

 double

### <a id="Jumbee_Console_BarChart_HorizontalBar_RemainingStyle"></a> RemainingStyle

The style of the remaining (unfilled) portion. Defaults to grey.

```csharp
public Style RemainingStyle { get; set; }
```

#### Property Value

 [Style](Jumbee.Console.Style.md)

### <a id="Jumbee_Console_BarChart_HorizontalBar_ShowRemaining"></a> ShowRemaining

Whether the remaining (unfilled) portion of the track is drawn. Defaults to <a href="https://learn.microsoft.com/dotnet/csharp/language-reference/builtin-types/bool">true</a>.

```csharp
public bool ShowRemaining { get; set; }
```

#### Property Value

 bool

### <a id="Jumbee_Console_BarChart_HorizontalBar_ShowValue"></a> ShowValue

Whether the formatted value is shown after the bar.

```csharp
public bool ShowValue { get; set; }
```

#### Property Value

 bool

### <a id="Jumbee_Console_BarChart_HorizontalBar_UnicodeBar"></a> UnicodeBar

The glyph used to draw the bar in Unicode mode.

```csharp
public char UnicodeBar { get; set; }
```

#### Property Value

 char

### <a id="Jumbee_Console_BarChart_HorizontalBar_Value"></a> Value

The bar's value.

```csharp
public double Value { get; set; }
```

#### Property Value

 double

### <a id="Jumbee_Console_BarChart_HorizontalBar_ValueFormatter"></a> ValueFormatter

An optional custom formatter for the value.

```csharp
public Func<double, CultureInfo, string>? ValueFormatter { get; set; }
```

#### Property Value

 Func<double, CultureInfo, string\>?

### <a id="Jumbee_Console_BarChart_HorizontalBar_Width"></a> Width

The bar's width in cells, or <a href="https://learn.microsoft.com/dotnet/csharp/language-reference/keywords/null">null</a> to fill the available width.

```csharp
public int? Width { get; set; }
```

#### Property Value

 int?

## Methods

### <a id="Jumbee_Console_BarChart_HorizontalBar_Measure_Spectre_Console_Rendering_RenderOptions_System_Int32_"></a> Measure\(RenderOptions, int\)

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

### <a id="Jumbee_Console_BarChart_HorizontalBar_Render_Spectre_Console_Rendering_RenderOptions_System_Int32_"></a> Render\(RenderOptions, int\)

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

