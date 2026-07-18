# <a id="Jumbee_Console_BarChart_BarChartItem"></a> Class BarChart.BarChartItem

Namespace: [Jumbee.Console](Jumbee.Console.md)  
Assembly: Jumbee.Console.dll  

A single labelled, coloured item in a <xref href="Jumbee.Console.BarChart" data-throw-if-not-resolved="false"></xref>.

```csharp
public class BarChart.BarChartItem
```

#### Inheritance

object ← 
[BarChart.BarChartItem](Jumbee.Console.BarChart.BarChartItem.md)

## Constructors

### <a id="Jumbee_Console_BarChart_BarChartItem__ctor_Jumbee_Console_BarChart_System_Int32_System_String_System_Double_Jumbee_Console_Color_"></a> BarChartItem\(BarChart, int, string, double, Color\)

Initializes a new <xref href="Jumbee.Console.BarChart.BarChartItem" data-throw-if-not-resolved="false"></xref> owned by <code class="paramref">chart</code> with the given index, label, value and colour.

```csharp
public BarChartItem(BarChart chart, int index, string label, double value, Color color)
```

#### Parameters

`chart` [BarChart](Jumbee.Console.BarChart.md)

`index` int

`label` string

`value` double

`color` [Color](Jumbee.Console.Color.md)

## Fields

### <a id="Jumbee_Console_BarChart_BarChartItem_Index"></a> Index

The item's stable index within its chart.

```csharp
public readonly int Index
```

#### Field Value

 int

## Properties

### <a id="Jumbee_Console_BarChart_BarChartItem_Chart"></a> Chart

The chart that owns this item, or <a href="https://learn.microsoft.com/dotnet/csharp/language-reference/keywords/null">null</a> once detached.

```csharp
public BarChart? Chart { get; }
```

#### Property Value

 [BarChart](Jumbee.Console.BarChart.md)?

### <a id="Jumbee_Console_BarChart_BarChartItem_Color"></a> Color

Gets the item color.

```csharp
public Color Color { get; set; }
```

#### Property Value

 [Color](Jumbee.Console.Color.md)

### <a id="Jumbee_Console_BarChart_BarChartItem_IsDetached"></a> IsDetached

Whether this item has been detached from its chart.

```csharp
public bool IsDetached { get; }
```

#### Property Value

 bool

### <a id="Jumbee_Console_BarChart_BarChartItem_Label"></a> Label

Gets the item label.

```csharp
public string Label { get; set; }
```

#### Property Value

 string

### <a id="Jumbee_Console_BarChart_BarChartItem_Value"></a> Value

Gets the item value.

```csharp
public double Value { get; set; }
```

#### Property Value

 double

## Methods

### <a id="Jumbee_Console_BarChart_BarChartItem_Detach"></a> Detach\(\)

Detaches this item from its chart so further changes no longer update it.

```csharp
public void Detach()
```

### <a id="Jumbee_Console_BarChart_BarChartItem_UpdateChart"></a> UpdateChart\(\)

Requests a redraw of the owning chart.

```csharp
public void UpdateChart()
```

