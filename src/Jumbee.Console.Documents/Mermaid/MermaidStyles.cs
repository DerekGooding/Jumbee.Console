namespace Jumbee.Console.Documents;

/// <summary>
/// Colours and scale for <see cref="MermaidViewer"/>. Colours are <see cref="Jumbee.Console.Color"/>; the two scale
/// divisors map Mermaider's SVG pixel layout to console cells (a node's pixel width — itself derived from its label
/// text — divided by <see cref="ScaleX"/> yields a cell width that fits the label).
/// </summary>
public sealed class MermaidStyles
{
    /// <summary>A shared default style set.</summary>
    public static MermaidStyles Default { get; } = new();

    /// <summary>Border colour for ordinary process nodes (rectangle / rounded).</summary>
    public Color NodeBorder { get; init; } = Color.DeepSkyBlue1;

    /// <summary>Border colour for decision nodes (drawn as a double-lined box instead of a diamond).</summary>
    public Color NodeDecision { get; init; } = Color.Orange1;

    /// <summary>Border colour for terminal nodes (circle / double-circle — start/end/connector).</summary>
    public Color NodeTerminal { get; init; } = Color.SpringGreen2;

    /// <summary>Border colour for special nodes (hexagon / cylinder / subroutine).</summary>
    public Color NodeSpecial { get; init; } = Color.MediumPurple1;

    public Color NodeLabel { get; init; } = Color.Grey93;
    public Color Edge { get; init; } = Color.Grey62;
    public Color Arrow { get; init; } = Color.Aquamarine1;
    public Color EdgeLabel { get; init; } = Color.Gold1;
    public Color GroupBorder { get; init; } = Color.Grey42;
    public Color GroupLabel { get; init; } = Color.Grey70;

    /// <summary>Class-diagram member text (attributes / methods).</summary>
    public Color Member { get; init; } = Color.Grey85;

    /// <summary>Class-diagram annotation, e.g. «interface».</summary>
    public Color Annotation { get; init; } = Color.Grey62;

    /// <summary>Pixels per cell column. ~9 ≈ the layout font's per-character advance, so boxes fit their labels.</summary>
    public double ScaleX { get; init; } = 9.0;

    /// <summary>Pixels per cell row. ~2×<see cref="ScaleX"/> keeps the terminal's ~2:1 cell aspect so diagrams aren't squashed.</summary>
    public double ScaleY { get; init; } = 18.0;
}
