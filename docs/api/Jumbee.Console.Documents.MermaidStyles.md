# <a id="Jumbee_Console_Documents_MermaidStyles"></a> Class MermaidStyles

Namespace: [Jumbee.Console.Documents](Jumbee.Console.Documents.md)  
Assembly: Jumbee.Console.Documents.dll  

Colours and scale for <xref href="Jumbee.Console.Documents.MermaidViewer" data-throw-if-not-resolved="false"></xref>.

```csharp
public sealed class MermaidStyles
```

#### Inheritance

object ← 
[MermaidStyles](Jumbee.Console.Documents.MermaidStyles.md)

## Remarks

Colours are <xref href="Jumbee.Console.Color" data-throw-if-not-resolved="false"></xref>; the two scale
divisors map Mermaider's SVG pixel layout to console cells (a node's pixel width — itself derived from its label
text — divided by <xref href="Jumbee.Console.Documents.MermaidStyles.ScaleX" data-throw-if-not-resolved="false"></xref> yields a cell width that fits the label).

## Properties

### <a id="Jumbee_Console_Documents_MermaidStyles_Annotation"></a> Annotation

Class-diagram annotation, e.g. «interface».

```csharp
public Color Annotation { get; init; }
```

#### Property Value

 [Color](Jumbee.Console.Color.md)

### <a id="Jumbee_Console_Documents_MermaidStyles_Arrow"></a> Arrow

Edge arrowhead colour.

```csharp
public Color Arrow { get; init; }
```

#### Property Value

 [Color](Jumbee.Console.Color.md)

### <a id="Jumbee_Console_Documents_MermaidStyles_Default"></a> Default

A shared default style set.

```csharp
public static MermaidStyles Default { get; }
```

#### Property Value

 [MermaidStyles](Jumbee.Console.Documents.MermaidStyles.md)

### <a id="Jumbee_Console_Documents_MermaidStyles_Edge"></a> Edge

Edge / connector line colour.

```csharp
public Color Edge { get; init; }
```

#### Property Value

 [Color](Jumbee.Console.Color.md)

### <a id="Jumbee_Console_Documents_MermaidStyles_EdgeLabel"></a> EdgeLabel

Edge label text colour.

```csharp
public Color EdgeLabel { get; init; }
```

#### Property Value

 [Color](Jumbee.Console.Color.md)

### <a id="Jumbee_Console_Documents_MermaidStyles_GroupBorder"></a> GroupBorder

Border colour of subgraphs / groups.

```csharp
public Color GroupBorder { get; init; }
```

#### Property Value

 [Color](Jumbee.Console.Color.md)

### <a id="Jumbee_Console_Documents_MermaidStyles_GroupLabel"></a> GroupLabel

Label text colour of subgraphs / groups.

```csharp
public Color GroupLabel { get; init; }
```

#### Property Value

 [Color](Jumbee.Console.Color.md)

### <a id="Jumbee_Console_Documents_MermaidStyles_Member"></a> Member

Class-diagram member text (attributes / methods).

```csharp
public Color Member { get; init; }
```

#### Property Value

 [Color](Jumbee.Console.Color.md)

### <a id="Jumbee_Console_Documents_MermaidStyles_NodeBorder"></a> NodeBorder

Border colour for ordinary process nodes (rectangle / rounded).

```csharp
public Color NodeBorder { get; init; }
```

#### Property Value

 [Color](Jumbee.Console.Color.md)

### <a id="Jumbee_Console_Documents_MermaidStyles_NodeDecision"></a> NodeDecision

Border colour for decision nodes (drawn as a double-lined box instead of a diamond).

```csharp
public Color NodeDecision { get; init; }
```

#### Property Value

 [Color](Jumbee.Console.Color.md)

### <a id="Jumbee_Console_Documents_MermaidStyles_NodeLabel"></a> NodeLabel

Node label text colour.

```csharp
public Color NodeLabel { get; init; }
```

#### Property Value

 [Color](Jumbee.Console.Color.md)

### <a id="Jumbee_Console_Documents_MermaidStyles_NodeSpecial"></a> NodeSpecial

Border colour for special nodes (hexagon / cylinder / subroutine).

```csharp
public Color NodeSpecial { get; init; }
```

#### Property Value

 [Color](Jumbee.Console.Color.md)

### <a id="Jumbee_Console_Documents_MermaidStyles_NodeTerminal"></a> NodeTerminal

Border colour for terminal nodes (circle / double-circle — start/end/connector).

```csharp
public Color NodeTerminal { get; init; }
```

#### Property Value

 [Color](Jumbee.Console.Color.md)

### <a id="Jumbee_Console_Documents_MermaidStyles_ScaleX"></a> ScaleX

Pixels per cell column. ~9 ≈ the layout font's per-character advance, so boxes fit their labels.

```csharp
public double ScaleX { get; init; }
```

#### Property Value

 double

### <a id="Jumbee_Console_Documents_MermaidStyles_ScaleY"></a> ScaleY

Pixels per cell row. ~2×<xref href="Jumbee.Console.Documents.MermaidStyles.ScaleX" data-throw-if-not-resolved="false"></xref> keeps the terminal's ~2:1 cell aspect so diagrams aren't squashed.

```csharp
public double ScaleY { get; init; }
```

#### Property Value

 double

