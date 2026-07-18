# <a id="Jumbee_Console_RunSeries"></a> Class RunSeries

Namespace: [Jumbee.Console](Jumbee.Console.md)  
Assembly: Jumbee.Console.dll  

A handle to one <xref href="Jumbee.Console.RunChart" data-throw-if-not-resolved="false"></xref> series: push values with <xref href="Jumbee.Console.RunSeries.Push(System.Double)" data-throw-if-not-resolved="false"></xref>. Tracks the current value, the
delta from the previous value, and the running max/min shown in the chart's legend.

```csharp
public sealed class RunSeries
```

#### Inheritance

object ← 
[RunSeries](Jumbee.Console.RunSeries.md)

## Remarks

Thread-safe (each push is marshaled onto the UI thread).

## Methods

### <a id="Jumbee_Console_RunSeries_Push_System_Double_"></a> Push\(double\)

Appends <code class="paramref">value</code>: scrolls it into the strip chart and updates cur/dlt/max/min.

```csharp
public void Push(double value)
```

#### Parameters

`value` double

