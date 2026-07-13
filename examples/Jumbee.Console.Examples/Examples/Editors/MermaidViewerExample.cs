namespace Jumbee.Console.Examples;

using System.Collections.Generic;

using Jumbee.Console.Documents;

/// <summary>
/// A read-only <see cref="MermaidViewer"/> rendering a Mermaid flowchart — node boxes, rectilinear edges with
/// arrowheads and labels — laid out by Mermaider and rasterized to box-drawing cells; scroll with arrows/PgUp/PgDn/wheel.
/// </summary>
public sealed class MermaidViewerExample : MermaidViewer, IExample
{
    public MermaidViewerExample() : base(SourceLoader.Read("sample.mmd")) { }

    #region IExample
    string IExample.Category => "Editors and Viewers";
    string IExample.Title => "Mermaid Viewer";
    string IExample.Description =>
        "Renders Mermaid flowchart / state diagrams as scrollable box-drawing graphics (parsed and laid out by Mermaider).";
    IReadOnlyList<string> IExample.SourceFiles => ["MermaidViewerExample.cs", "MermaidViewer.cs", "sample.mmd"];
    #endregion
}
