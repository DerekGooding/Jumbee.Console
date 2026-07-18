# <a id="Jumbee_Console_HScroll"></a> Struct HScroll

Namespace: [Jumbee.Console](Jumbee.Console.md)  
Assembly: Jumbee.Console.dll  

An opt-in horizontal-scroll offset for controls that render a fixed-width buffer wider than their viewport and
window it in <code>Blit</code> (the <xref href="Jumbee.Console.ControlFrame" data-throw-if-not-resolved="false"></xref> only scrolls vertically).

```csharp
public struct HScroll
```

## Examples

<pre><code class="lang-csharp">private HScroll _hscroll;
// in Blit:   var left = _hscroll.Clamp(content.Width, viewportWidth); ... src[x + left, y]
// in OnInput: if (_hscroll.Pan(±step, content.Width, viewportWidth)) Invalidate();
// on new content / Home: _hscroll.Reset();</code></pre>

## Remarks

Hold one as a mutable field; call <xref href="Jumbee.Console.HScroll.Clamp(System.Int32%2cSystem.Int32)" data-throw-if-not-resolved="false"></xref> when blitting and <xref href="Jumbee.Console.HScroll.Pan(System.Int32%2cSystem.Int32%2cSystem.Int32)" data-throw-if-not-resolved="false"></xref> from left/right key
handling. The content and viewport widths are passed per call rather than stored, since they change with
layout/resize.

## Properties

### <a id="Jumbee_Console_HScroll_Offset"></a> Offset

The current offset — the leftmost visible content column.

```csharp
public readonly int Offset { get; }
```

#### Property Value

 int

## Methods

### <a id="Jumbee_Console_HScroll_Clamp_System_Int32_System_Int32_"></a> Clamp\(int, int\)

Re-clamps to the current widths (e.g. a resize widened the viewport) and returns the offset. Call in Blit.

```csharp
public int Clamp(int contentWidth, int viewportWidth)
```

#### Parameters

`contentWidth` int

`viewportWidth` int

#### Returns

 int

### <a id="Jumbee_Console_HScroll_Max_System_Int32_System_Int32_"></a> Max\(int, int\)

The largest valid offset for the given widths (0 when the content fits).

```csharp
public static int Max(int contentWidth, int viewportWidth)
```

#### Parameters

`contentWidth` int

`viewportWidth` int

#### Returns

 int

### <a id="Jumbee_Console_HScroll_Pan_System_Int32_System_Int32_System_Int32_"></a> Pan\(int, int, int\)

Pans by <code class="paramref">delta</code> columns, clamped to the content/viewport. Returns whether it moved.

```csharp
public bool Pan(int delta, int contentWidth, int viewportWidth)
```

#### Parameters

`delta` int

`contentWidth` int

`viewportWidth` int

#### Returns

 bool

### <a id="Jumbee_Console_HScroll_Reset"></a> Reset\(\)

Scrolls back to the left edge (new content, Home).

```csharp
public void Reset()
```

### <a id="Jumbee_Console_HScroll_SetOffset_System_Int32_System_Int32_System_Int32_"></a> SetOffset\(int, int, int\)

Sets the offset, clamped to <code>[0, Max]</code>. Returns whether it changed.

```csharp
public bool SetOffset(int value, int contentWidth, int viewportWidth)
```

#### Parameters

`value` int

`contentWidth` int

`viewportWidth` int

#### Returns

 bool

