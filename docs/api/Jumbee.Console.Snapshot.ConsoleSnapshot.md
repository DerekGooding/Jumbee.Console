# <a id="Jumbee_Console_Snapshot_ConsoleSnapshot"></a> Class ConsoleSnapshot

Namespace: [Jumbee.Console.Snapshot](Jumbee.Console.Snapshot.md)  
Assembly: Jumbee.Console.Snapshot.dll  

Renders Jumbee.Console controls headlessly (without a real terminal) to a <xref href="Jumbee.Console.ConsoleBuffer" data-throw-if-not-resolved="false"></xref>,
and converts that buffer to a text or PNG snapshot. Intended for tests and visual verification.

```csharp
public static class ConsoleSnapshot
```

#### Inheritance

object ← 
[ConsoleSnapshot](Jumbee.Console.Snapshot.ConsoleSnapshot.md)

## Methods

### <a id="Jumbee_Console_Snapshot_ConsoleSnapshot_Key_System_ConsoleKey_System_Boolean_System_Boolean_System_Boolean_"></a> Key\(ConsoleKey, bool, bool, bool\)

Builds a <xref href="System.ConsoleKeyInfo" data-throw-if-not-resolved="false"></xref> for a key with optional modifiers. For letter and digit keys
    the <code>KeyChar</code> is filled in (lowercase, uppercase under Shift, the control char under Ctrl) so the result
    matches how a hotkey registered with <xref href="Jumbee.Console.UI.RegisterHotKey(System.ConsoleKeyInfo%2cSystem.Action)" data-throw-if-not-resolved="false"></xref> — or a real keystroke — is keyed. That
    matters for <xref href="Jumbee.Console.Snapshot.ConsoleSnapshot.RenderAfter(Jumbee.Console.Control%2cSystem.Int32%2cSystem.Int32%2cSystem.Collections.Generic.IReadOnlyList%7bSystem.ConsoleKeyInfo%7d%2cSystem.Boolean)" data-throw-if-not-resolved="false"></xref> with
    <code>routeGlobal</code>: a bare-letter global hotkey only fires when the simulated key's char matches. Non-character
    keys (arrows, function keys, …) keep <code>'\0'</code>. For a punctuation hotkey (e.g. <code>'/'</code>), this method's
    char is <code>'\0'</code> and won't match — use <code>UI.HotKeys.Char('/')</code> to build the key instead.

```csharp
public static ConsoleKeyInfo Key(ConsoleKey key, bool shift = false, bool alt = false, bool control = false)
```

#### Parameters

`key` ConsoleKey

`shift` bool

`alt` bool

`control` bool

#### Returns

 ConsoleKeyInfo

### <a id="Jumbee_Console_Snapshot_ConsoleSnapshot_Render_ConsoleGUI_IControl_System_Int32_System_Int32_"></a> Render\(IControl, int, int\)

Composes a control tree into a <xref href="Jumbee.Console.ConsoleBuffer" data-throw-if-not-resolved="false"></xref> at the given size, without a real console.

```csharp
public static ConsoleBuffer Render(IControl content, int width, int height)
```

#### Parameters

`content` IControl

`width` int

`height` int

#### Returns

 [ConsoleBuffer](Jumbee.Console.ConsoleBuffer.md)

### <a id="Jumbee_Console_Snapshot_ConsoleSnapshot_Render_Jumbee_Console_Control_System_Int32_System_Int32_"></a> Render\(Control, int, int\)

Composes a single control (using its frame when present) into a buffer.

```csharp
public static ConsoleBuffer Render(Control control, int width, int height)
```

#### Parameters

`control` [Control](Jumbee.Console.Control.md)

`width` int

`height` int

#### Returns

 [ConsoleBuffer](Jumbee.Console.ConsoleBuffer.md)

### <a id="Jumbee_Console_Snapshot_ConsoleSnapshot_Render_Jumbee_Console_ILayout_System_Int32_System_Int32_"></a> Render\(ILayout, int, int\)

Composes a layout into a buffer.

```csharp
public static ConsoleBuffer Render(ILayout layout, int width, int height)
```

#### Parameters

`layout` [ILayout](Jumbee.Console.ILayout.md)

`width` int

`height` int

#### Returns

 [ConsoleBuffer](Jumbee.Console.ConsoleBuffer.md)

### <a id="Jumbee_Console_Snapshot_ConsoleSnapshot_RenderAfter_Jumbee_Console_Control_System_Int32_System_Int32_System_ConsoleKey___"></a> RenderAfter\(Control, int, int, params ConsoleKey\[\]\)

Renders <code class="paramref">control</code> once to establish layout, sends the given keys to it (routed via
<xref href="Jumbee.Console.UI.SendInput(Jumbee.Console.IFocusable%2cSystem.ConsoleKeyInfo%2cSystem.Boolean)" data-throw-if-not-resolved="false"></xref>), then renders and returns the result.

```csharp
public static ConsoleBuffer RenderAfter(Control control, int width, int height, params ConsoleKey[] keys)
```

#### Parameters

`control` [Control](Jumbee.Console.Control.md)

`width` int

`height` int

`keys` ConsoleKey\[\]

#### Returns

 [ConsoleBuffer](Jumbee.Console.ConsoleBuffer.md)

#### Remarks

Handy for snapshotting a control after navigation/editing. The keys are delivered to
    <code class="paramref">control</code> itself — <em>not</em> to whatever <xref href="Jumbee.Console.UI.SetFocus(Jumbee.Console.IFocusable)" data-throw-if-not-resolved="false"></xref> last targeted
    elsewhere in the tree — so pass the control that actually changes. For a composite app, that's the specific
    child under test (e.g. the list), not the root layout.

### <a id="Jumbee_Console_Snapshot_ConsoleSnapshot_RenderAfter_Jumbee_Console_Control_System_Int32_System_Int32_System_Collections_Generic_IReadOnlyList_System_ConsoleKeyInfo__System_Boolean_"></a> RenderAfter\(Control, int, int, IReadOnlyList<ConsoleKeyInfo\>, bool\)

As <xref href="Jumbee.Console.Snapshot.ConsoleSnapshot.RenderAfter(Jumbee.Console.Control%2cSystem.Int32%2cSystem.Int32%2cSystem.ConsoleKey%5b%5d)" data-throw-if-not-resolved="false"></xref> but accepts full key info, so modifier
keys (e.g. <code>Alt+Down</code> via <xref href="Jumbee.Console.Snapshot.ConsoleSnapshot.Key(System.ConsoleKey%2cSystem.Boolean%2cSystem.Boolean%2cSystem.Boolean)" data-throw-if-not-resolved="false"></xref>) can be sent. When <code class="paramref">routeGlobal</code> is
<a href="https://learn.microsoft.com/dotnet/csharp/language-reference/builtin-types/bool">true</a>, each key runs the global hotkey dispatch first (see
<xref href="Jumbee.Console.UI.SendInput(Jumbee.Console.IFocusable%2cSystem.ConsoleKeyInfo%2cSystem.Boolean)" data-throw-if-not-resolved="false"></xref>) so a snapshot can exercise hotkeys registered
with <xref href="Jumbee.Console.UI.RegisterHotKey(System.ConsoleKeyInfo%2cSystem.Action)" data-throw-if-not-resolved="false"></xref> — build the keys the same way they were registered (e.g. with
<xref href="Jumbee.Console.UI.HotKeys" data-throw-if-not-resolved="false"></xref>). As with the other overload, the keys go to <code class="paramref">control</code> itself,
not to whatever <xref href="Jumbee.Console.UI.SetFocus(Jumbee.Console.IFocusable)" data-throw-if-not-resolved="false"></xref> designates.

```csharp
public static ConsoleBuffer RenderAfter(Control control, int width, int height, IReadOnlyList<ConsoleKeyInfo> keys, bool routeGlobal = false)
```

#### Parameters

`control` [Control](Jumbee.Console.Control.md)

`width` int

`height` int

`keys` IReadOnlyList<ConsoleKeyInfo\>

`routeGlobal` bool

#### Returns

 [ConsoleBuffer](Jumbee.Console.ConsoleBuffer.md)

### <a id="Jumbee_Console_Snapshot_ConsoleSnapshot_SavePng_Jumbee_Console_ConsoleBuffer_System_String_Jumbee_Console_Snapshot_SnapshotImageOptions_"></a> SavePng\(ConsoleBuffer, string, SnapshotImageOptions?\)

Renders a buffer to a PNG file.

```csharp
public static void SavePng(ConsoleBuffer buffer, string path, SnapshotImageOptions? options = null)
```

#### Parameters

`buffer` [ConsoleBuffer](Jumbee.Console.ConsoleBuffer.md)

`path` string

`options` [SnapshotImageOptions](Jumbee.Console.Snapshot.SnapshotImageOptions.md)?

### <a id="Jumbee_Console_Snapshot_ConsoleSnapshot_SavePng_Jumbee_Console_Control_System_Int32_System_Int32_System_String_Jumbee_Console_Snapshot_SnapshotImageOptions_"></a> SavePng\(Control, int, int, string, SnapshotImageOptions?\)

Renders a control and saves it to a PNG file.

```csharp
public static void SavePng(Control control, int width, int height, string path, SnapshotImageOptions? options = null)
```

#### Parameters

`control` [Control](Jumbee.Console.Control.md)

`width` int

`height` int

`path` string

`options` [SnapshotImageOptions](Jumbee.Console.Snapshot.SnapshotImageOptions.md)?

### <a id="Jumbee_Console_Snapshot_ConsoleSnapshot_SavePng_Jumbee_Console_ILayout_System_Int32_System_Int32_System_String_Jumbee_Console_Snapshot_SnapshotImageOptions_"></a> SavePng\(ILayout, int, int, string, SnapshotImageOptions?\)

Renders a layout and saves it to a PNG file.

```csharp
public static void SavePng(ILayout layout, int width, int height, string path, SnapshotImageOptions? options = null)
```

#### Parameters

`layout` [ILayout](Jumbee.Console.ILayout.md)

`width` int

`height` int

`path` string

`options` [SnapshotImageOptions](Jumbee.Console.Snapshot.SnapshotImageOptions.md)?

### <a id="Jumbee_Console_Snapshot_ConsoleSnapshot_SavePngAfter_Jumbee_Console_Control_System_Int32_System_Int32_System_String_System_ConsoleKey___"></a> SavePngAfter\(Control, int, int, string, params ConsoleKey\[\]\)

Renders a control after sending the given keys and saves it to a PNG file.

```csharp
public static void SavePngAfter(Control control, int width, int height, string path, params ConsoleKey[] keys)
```

#### Parameters

`control` [Control](Jumbee.Console.Control.md)

`width` int

`height` int

`path` string

`keys` ConsoleKey\[\]

### <a id="Jumbee_Console_Snapshot_ConsoleSnapshot_SavePngAfter_Jumbee_Console_Control_System_Int32_System_Int32_System_String_System_Collections_Generic_IReadOnlyList_System_ConsoleKeyInfo__"></a> SavePngAfter\(Control, int, int, string, IReadOnlyList<ConsoleKeyInfo\>\)

Renders a control after sending the given keys (with modifiers) and saves it to a PNG file.

```csharp
public static void SavePngAfter(Control control, int width, int height, string path, IReadOnlyList<ConsoleKeyInfo> keys)
```

#### Parameters

`control` [Control](Jumbee.Console.Control.md)

`width` int

`height` int

`path` string

`keys` IReadOnlyList<ConsoleKeyInfo\>

### <a id="Jumbee_Console_Snapshot_ConsoleSnapshot_ToImage_Jumbee_Console_ConsoleBuffer_Jumbee_Console_Snapshot_SnapshotImageOptions_"></a> ToImage\(ConsoleBuffer, SnapshotImageOptions?\)

Renders a buffer to an image, drawing each cell's glyph and colors.

```csharp
public static Image<Rgba32> ToImage(ConsoleBuffer buffer, SnapshotImageOptions? options = null)
```

#### Parameters

`buffer` [ConsoleBuffer](Jumbee.Console.ConsoleBuffer.md)

`options` [SnapshotImageOptions](Jumbee.Console.Snapshot.SnapshotImageOptions.md)?

#### Returns

 Image<Rgba32\>

### <a id="Jumbee_Console_Snapshot_ConsoleSnapshot_ToText_Jumbee_Console_ConsoleBuffer_"></a> ToText\(ConsoleBuffer\)

Converts a buffer to a plain-text snapshot (glyphs only, one line per row). Colour and text
    decoration are NOT captured, so state distinguished only by colour (e.g. a dimmed "read" row) is invisible to
    a text assertion — use <code>ToImage</code>/<code>SavePng</code> for colour, or render a visible glyph marker to assert on
    with text.

```csharp
public static string ToText(ConsoleBuffer buffer)
```

#### Parameters

`buffer` [ConsoleBuffer](Jumbee.Console.ConsoleBuffer.md)

#### Returns

 string

### <a id="Jumbee_Console_Snapshot_ConsoleSnapshot_ToText_Jumbee_Console_Control_System_Int32_System_Int32_"></a> ToText\(Control, int, int\)

Renders a control and returns its text snapshot.

```csharp
public static string ToText(Control control, int width, int height)
```

#### Parameters

`control` [Control](Jumbee.Console.Control.md)

`width` int

`height` int

#### Returns

 string

### <a id="Jumbee_Console_Snapshot_ConsoleSnapshot_ToText_Jumbee_Console_ILayout_System_Int32_System_Int32_"></a> ToText\(ILayout, int, int\)

Renders a layout and returns its text snapshot.

```csharp
public static string ToText(ILayout layout, int width, int height)
```

#### Parameters

`layout` [ILayout](Jumbee.Console.ILayout.md)

`width` int

`height` int

#### Returns

 string

### <a id="Jumbee_Console_Snapshot_ConsoleSnapshot_ToTextAfter_Jumbee_Console_Control_System_Int32_System_Int32_System_ConsoleKey___"></a> ToTextAfter\(Control, int, int, params ConsoleKey\[\]\)

Renders a control after sending the given keys and returns its text snapshot.

```csharp
public static string ToTextAfter(Control control, int width, int height, params ConsoleKey[] keys)
```

#### Parameters

`control` [Control](Jumbee.Console.Control.md)

`width` int

`height` int

`keys` ConsoleKey\[\]

#### Returns

 string

### <a id="Jumbee_Console_Snapshot_ConsoleSnapshot_ToTextAfter_Jumbee_Console_Control_System_Int32_System_Int32_System_Collections_Generic_IReadOnlyList_System_ConsoleKeyInfo__System_Boolean_"></a> ToTextAfter\(Control, int, int, IReadOnlyList<ConsoleKeyInfo\>, bool\)

Renders a control after sending the given keys (with modifiers) and returns its text snapshot.
    Pass <code class="paramref">routeGlobal</code> to run each key through the global hotkey dispatch first (see
    <xref href="Jumbee.Console.Snapshot.ConsoleSnapshot.RenderAfter(Jumbee.Console.Control%2cSystem.Int32%2cSystem.Int32%2cSystem.Collections.Generic.IReadOnlyList%7bSystem.ConsoleKeyInfo%7d%2cSystem.Boolean)" data-throw-if-not-resolved="false"></xref>).

```csharp
public static string ToTextAfter(Control control, int width, int height, IReadOnlyList<ConsoleKeyInfo> keys, bool routeGlobal = false)
```

#### Parameters

`control` [Control](Jumbee.Console.Control.md)

`width` int

`height` int

`keys` IReadOnlyList<ConsoleKeyInfo\>

`routeGlobal` bool

#### Returns

 string

