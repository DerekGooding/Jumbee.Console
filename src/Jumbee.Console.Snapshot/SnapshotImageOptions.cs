namespace Jumbee.Console.Snapshot;
/// <summary>
/// Options controlling how a <see cref="ConsoleBuffer"/> is rendered to a PNG image.
/// </summary>
public sealed class SnapshotImageOptions
{
    /// <summary>Preferred monospace font family. Falls back to other common monospace fonts if unavailable.</summary>
    /// <remarks>The default (Consolas) has no Braille (U+2800–U+28FF) glyphs, so a PNG of Braille-drawn output — a
    /// <c>Plot</c>/<c>Canvas</c> with the Braille brush/marker — renders those cells as missing-glyph boxes. To
    /// snapshot Braille output visibly, set this to a Braille-covering font such as <c>"Cascadia Mono"</c> or
    /// <c>"DejaVu Sans Mono"</c>. (Text snapshots via <c>ConsoleSnapshot.ToText</c> are unaffected — they capture the
    /// raw glyphs, not a rasterised font.)</remarks>
    public string FontFamily { get; set; } = "Consolas";

    /// <summary>Font size in points used to draw glyphs.</summary>
    public float FontSize { get; set; } = 16f;

    /// <summary>Width in pixels of a single character cell.</summary>
    public int CellWidth { get; set; } = 10;

    /// <summary>Height in pixels of a single character cell.</summary>
    public int CellHeight { get; set; } = 20;

    /// <summary>Padding in pixels around the rendered grid.</summary>
    public int Padding { get; set; } = 8;

    /// <summary>Foreground color used when a cell has no explicit foreground.</summary>
    public SixLabors.ImageSharp.Color DefaultForeground { get; set; } = SixLabors.ImageSharp.Color.FromRgb(204, 204, 204);

    /// <summary>Background color used for the canvas and cells with no explicit background.</summary>
    public SixLabors.ImageSharp.Color DefaultBackground { get; set; } = SixLabors.ImageSharp.Color.FromRgb(12, 12, 12);

    /// <summary>Fallback monospace font families tried in order when <see cref="FontFamily"/> is not found.</summary>
    public IReadOnlyList<string> FallbackFontFamilies { get; set; } =
        ["Consolas", "Cascadia Mono", "Cascadia Code", "Courier New", "DejaVu Sans Mono", "Liberation Mono"];
}