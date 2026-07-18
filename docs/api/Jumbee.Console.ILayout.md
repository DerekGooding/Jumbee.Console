# <a id="Jumbee_Console_ILayout"></a> Interface ILayout

Namespace: [Jumbee.Console](Jumbee.Console.md)  
Assembly: Jumbee.Console.dll  

Common interface for Jumbee.Console layout classes: a 2-D grid of focusable cells over a ConsoleGUI control, with focus navigation and input routing.

```csharp
public interface ILayout : IFocusable
```

#### Implements

[IFocusable](Jumbee.Console.IFocusable.md)

## Properties

### <a id="Jumbee_Console_ILayout_CControl"></a> CControl

The underlying ConsoleGUI control this layout wraps.

```csharp
IControl CControl { get; }
```

#### Property Value

 IControl

### <a id="Jumbee_Console_ILayout_Columns"></a> Columns

The number of columns in the layout grid.

```csharp
int Columns { get; }
```

#### Property Value

 int

### <a id="Jumbee_Console_ILayout_Controls"></a> Controls

The focusables in every grid cell, in row-major order.

```csharp
IEnumerable<IFocusable> Controls { get; }
```

#### Property Value

 IEnumerable<[IFocusable](Jumbee.Console.IFocusable.md)\>

### <a id="Jumbee_Console_ILayout_Rows"></a> Rows

The number of rows in the layout grid.

```csharp
int Rows { get; }
```

#### Property Value

 int

### <a id="Jumbee_Console_ILayout_Item_System_Int32_System_Int32_"></a> this\[int, int\]

Gets the focusable at the given grid cell.

```csharp
IFocusable this[int row, int column] { get; }
```

#### Property Value

 [IFocusable](Jumbee.Console.IFocusable.md)

## Methods

### <a id="Jumbee_Console_ILayout_CellAt_System_Int32_System_Int32_"></a> CellAt\(int, int\)

Indexer access that tolerates an empty slot (a sparse Grid cell can throw from the underlying
    indexer); returns <a href="https://learn.microsoft.com/dotnet/csharp/language-reference/keywords/null">null</a> in that case.

```csharp
IFocusable? CellAt(int row, int column)
```

#### Parameters

`row` int

`column` int

#### Returns

 [IFocusable](Jumbee.Console.IFocusable.md)?

### <a id="Jumbee_Console_ILayout_FirstLeaf_Jumbee_Console_IFocusable_"></a> FirstLeaf\(IFocusable?\)

The first focusable leaf reachable from <code class="paramref">node</code> (see <xref href="Jumbee.Console.ILayout.Leaves(Jumbee.Console.IFocusable)" data-throw-if-not-resolved="false"></xref>), or null.

```csharp
public static IFocusable? FirstLeaf(IFocusable? node)
```

#### Parameters

`node` [IFocusable](Jumbee.Console.IFocusable.md)?

#### Returns

 [IFocusable](Jumbee.Console.IFocusable.md)?

### <a id="Jumbee_Console_ILayout_FocusedCell"></a> FocusedCell\(\)

The (row, column) of the cell whose subtree currently holds focus, or <a href="https://learn.microsoft.com/dotnet/csharp/language-reference/keywords/null">null</a> if none.

```csharp
(int Row, int Column)? FocusedCell()
```

#### Returns

 \(int Row, int Column\)?

### <a id="Jumbee_Console_ILayout_Leaves_Jumbee_Console_IFocusable_"></a> Leaves\(IFocusable?\)

The focusable leaves reachable from <code class="paramref">node</code>, in order: descends nested layouts and
    frames; a leaf is an interactive, laid-out Control (Focusable + HandlesInput + HasLayout). A composite is an
    opaque leaf here (it reports HandlesInput) and manages its own children's focus internally.

```csharp
public static IEnumerable<IFocusable> Leaves(IFocusable? node)
```

#### Parameters

`node` [IFocusable](Jumbee.Console.IFocusable.md)?

#### Returns

 IEnumerable<[IFocusable](Jumbee.Console.IFocusable.md)\>

### <a id="Jumbee_Console_ILayout_RegionCycleTarget_System_Int32_Jumbee_Console_IFocusable_"></a> RegionCycleTarget\(int, IFocusable?\)

Computes the next (<code class="paramref">direction</code> &gt; 0) or previous focusable within the currently
    focused region, relative to <code class="paramref">current</code>, wrapping — or <a href="https://learn.microsoft.com/dotnet/csharp/language-reference/keywords/null">null</a>.

```csharp
IFocusable? RegionCycleTarget(int direction, IFocusable? current)
```

#### Parameters

`direction` int

`current` [IFocusable](Jumbee.Console.IFocusable.md)?

#### Returns

 [IFocusable](Jumbee.Console.IFocusable.md)?

#### Remarks

A no-op (null) unless the focused cell is a multi-focusable nested layout (enter/leave a single
    control or composite via the spatial arrows instead).

### <a id="Jumbee_Console_ILayout_SpatialTarget_System_Int32_System_Int32_"></a> SpatialTarget\(int, int\)

Computes the focusable one cell in the given direction from the focused cell (wraps per axis; skips
    empty cells), or <a href="https://learn.microsoft.com/dotnet/csharp/language-reference/keywords/null">null</a> if none. Pass a row/column delta (e.g. (0, -1) = left). Landing on a
    cell descends to its first focusable leaf.

```csharp
IFocusable? SpatialTarget(int dRow, int dCol)
```

#### Parameters

`dRow` int

`dCol` int

#### Returns

 [IFocusable](Jumbee.Console.IFocusable.md)?

### <a id="Jumbee_Console_ILayout_SpatialTarget_System_Int32_System_Int32_System_Boolean_"></a> SpatialTarget\(int, int, bool\)

The directional move, made recursive so arrows cross cells nested several layouts deep (e.g. panes inside
nested split panels).

```csharp
IFocusable? SpatialTarget(int dRow, int dCol, bool wrap)
```

#### Parameters

`dRow` int

`dCol` int

`wrap` bool

#### Returns

 [IFocusable](Jumbee.Console.IFocusable.md)?

#### Remarks

It first descends into the focused nested layout and lets it move within itself; that nested move never
wraps, so at the nested edge it returns <a href="https://learn.microsoft.com/dotnet/csharp/language-reference/keywords/null">null</a> and we step at this level instead — the arrow
"exits" the nested layout to the parent's sibling cell. The top-level call wraps per axis (the
region-to-region behavior); nested calls do not.

