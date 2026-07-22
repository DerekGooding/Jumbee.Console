# <a id="Jumbee_Console_Snapshot_SnapshotImageOptions"></a> Class SnapshotImageOptions

Namespace: [Jumbee.Console.Snapshot](Jumbee.Console.Snapshot.md)  
Assembly: Jumbee.Console.Snapshot.dll  

Options controlling how a <xref href="Jumbee.Console.ConsoleBuffer" data-throw-if-not-resolved="false"></xref> is rendered to a PNG image.

```csharp
public sealed class SnapshotImageOptions
```

#### Inheritance

object ← 
[SnapshotImageOptions](Jumbee.Console.Snapshot.SnapshotImageOptions.md)

## Properties

### <a id="Jumbee_Console_Snapshot_SnapshotImageOptions_CellHeight"></a> CellHeight

Height in pixels of a single character cell.

```csharp
public int CellHeight { get; set; }
```

#### Property Value

 int

### <a id="Jumbee_Console_Snapshot_SnapshotImageOptions_CellWidth"></a> CellWidth

Width in pixels of a single character cell.

```csharp
public int CellWidth { get; set; }
```

#### Property Value

 int

### <a id="Jumbee_Console_Snapshot_SnapshotImageOptions_DefaultBackground"></a> DefaultBackground

Background color used for the canvas and cells with no explicit background.

```csharp
public Color DefaultBackground { get; set; }
```

#### Property Value

 Color

### <a id="Jumbee_Console_Snapshot_SnapshotImageOptions_DefaultForeground"></a> DefaultForeground

Foreground color used when a cell has no explicit foreground.

```csharp
public Color DefaultForeground { get; set; }
```

#### Property Value

 Color

### <a id="Jumbee_Console_Snapshot_SnapshotImageOptions_FallbackFontFamilies"></a> FallbackFontFamilies

Fallback monospace font families tried in order when <xref href="Jumbee.Console.Snapshot.SnapshotImageOptions.FontFamily" data-throw-if-not-resolved="false"></xref> is not found.

```csharp
public IReadOnlyList<string> FallbackFontFamilies { get; set; }
```

#### Property Value

 IReadOnlyList<string\>

### <a id="Jumbee_Console_Snapshot_SnapshotImageOptions_FontFamily"></a> FontFamily

Preferred monospace font family. Falls back to other common monospace fonts if unavailable.

```csharp
public string FontFamily { get; set; }
```

#### Property Value

 string

#### Remarks

The default (Consolas) has no Braille (U+2800–U+28FF) glyphs, so a PNG of Braille-drawn output — a
    <code>Plot</code>/<code>Canvas</code> with the Braille brush/marker — renders those cells as missing-glyph boxes. To
    snapshot Braille output visibly, set this to a Braille-covering font such as <code>"Cascadia Mono"</code> or
    <code>"DejaVu Sans Mono"</code>. (Text snapshots via <code>ConsoleSnapshot.ToText</code> are unaffected — they capture the
    raw glyphs, not a rasterised font.)

### <a id="Jumbee_Console_Snapshot_SnapshotImageOptions_FontSize"></a> FontSize

Font size in points used to draw glyphs.

```csharp
public float FontSize { get; set; }
```

#### Property Value

 float

### <a id="Jumbee_Console_Snapshot_SnapshotImageOptions_Padding"></a> Padding

Padding in pixels around the rendered grid.

```csharp
public int Padding { get; set; }
```

#### Property Value

 int

