namespace Jumbee.Console.Examples;

using System.Collections.Generic;

using Jumbee.Console.Documents;

/// <summary>
/// The <see cref="MarkdownExtendedViewer"/> rendering Markdown with embedded <c>```mermaid</c> diagrams — prose,
/// lists and syntax-highlighted code interleaved with box-drawn flowchart and sequence diagrams; scroll with the
/// arrows, PgUp/PgDn, Home/End and the wheel.
/// </summary>
public sealed class MarkdownExtendedViewerExample : MarkdownExtendedViewer, IExample
{
    public MarkdownExtendedViewerExample() : base(SourceLoader.Read("sample-md-mermaid.md")) { }

    #region IExample
    string IExample.Category => "Editors and Viewers";
    string IExample.Title => "Markdown + Mermaid";
    string IExample.Description =>
        "Renders Markdown with embedded mermaid diagrams — fenced ```mermaid blocks are drawn as diagrams inline between the prose, lists and code.";
    IReadOnlyList<string> IExample.SourceFiles =>
        ["MarkdownExtendedViewerExample.cs", "MarkdownExtendedViewer.cs", "sample-md-mermaid.md"];
    #endregion
}
