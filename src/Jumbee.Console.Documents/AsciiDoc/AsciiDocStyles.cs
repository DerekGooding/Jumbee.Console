namespace Jumbee.Console.Documents;

/// <summary>
/// Visual styling for <see cref="AsciiDocViewer"/>.
/// </summary>
/// <remarks>
/// Each value is a Spectre.Console markup style string
/// (e.g. <c>"bold blue"</c>) applied to the console renderables emitted while traversing the AsciiDoc AST.
/// </remarks>
public sealed class AsciiDocStyles
{
    /// <summary>A shared default style set.</summary>
    public static AsciiDocStyles Default { get; } = new();

    /// <summary>The document title (<c>= Title</c> header), rendered as a horizontal rule.</summary>
    public string Title { get; init; } = "bold white";

    /// <summary>Section heading styles indexed by level (1 = <c>==</c>, 2 = <c>===</c>, …). Index 0 is unused.</summary>
    public string[] Headings { get; init; } =
        ["bold white", "bold deepskyblue1", "bold aquamarine1", "bold mediumpurple1", "bold gold1", "bold grey70", "bold grey70"];

    /// <summary>Bold (<c>*strong*</c>) inline text.</summary>
    public string Strong { get; init; } = "bold";

    /// <summary>Emphasised (<c>_italic_</c>) inline text.</summary>
    public string Emphasis { get; init; } = "italic";

    /// <summary>Inline monospace (<c>`code`</c>) text.</summary>
    public string Monospace { get; init; } = "lightgreen";

    /// <summary>Highlighted (<c>#mark#</c>) inline text.</summary>
    public string Highlight { get; init; } = "black on yellow";

    /// <summary>Hyperlink text.</summary>
    public string Link { get; init; } = "blue underline";

    /// <summary>Cross-reference (<c>&lt;&lt;ref&gt;&gt;</c>) text.</summary>
    public string CrossReference { get; init; } = "aquamarine1";

    /// <summary>List bullet / ordered-list markers.</summary>
    public string ListMarker { get; init; } = "grey70";

    /// <summary>Foreground of verbatim source / listing / literal block text.</summary>
    public string Code { get; init; } = "grey85";

    /// <summary>Quote / verse block body text.</summary>
    public string Quote { get; init; } = "italic grey78";

    /// <summary>Quote attribution (author / source) line.</summary>
    public string Attribution { get; init; } = "grey62";

    /// <summary>Border colour of code / example / sidebar panels and thematic breaks.</summary>
    public string PanelBorder { get; init; } = "grey42";

    /// <summary>Colours/scale for embedded <c>[source,mermaid]</c> diagram blocks.</summary>
    public MermaidStyles Mermaid { get; init; } = MermaidStyles.Default;

    /// <summary>Accent (border) colours for the five admonition kinds.</summary>
    public string AdmonitionNote { get; init; } = "deepskyblue1";

    /// <summary>Accent (border) colour of TIP admonitions.</summary>
    public string AdmonitionTip { get; init; } = "green";

    /// <summary>Accent (border) colour of IMPORTANT admonitions.</summary>
    public string AdmonitionImportant { get; init; } = "mediumpurple1";

    /// <summary>Accent (border) colour of WARNING admonitions.</summary>
    public string AdmonitionWarning { get; init; } = "gold1";

    /// <summary>Accent (border) colour of CAUTION admonitions.</summary>
    public string AdmonitionCaution { get; init; } = "red";
}