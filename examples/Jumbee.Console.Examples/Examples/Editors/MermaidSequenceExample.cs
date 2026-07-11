namespace Jumbee.Console.Examples;

using System.Collections.Generic;

using Jumbee.Console.DocumentViewers;

/// <summary>
/// The <see cref="MermaidViewer"/> rendering a Mermaid <c>sequenceDiagram</c> — actor boxes with lifelines, stacked
/// message arrows (solid/dashed), notes and a loop frame; scroll with arrows/PgUp/PgDn/wheel.
/// </summary>
public sealed class MermaidSequenceExample : MermaidViewer, IExample
{
    public MermaidSequenceExample() : base(SourceLoader.Read("sample-seq.mmd")) { }

    #region IExample
    string IExample.Category => "Editors and Viewers";
    string IExample.Title => "Mermaid Sequence Diagram";
    string IExample.Description =>
        "Renders a Mermaid sequence diagram — lifelines, message arrows, notes and block frames — as scrollable box-drawing graphics.";
    IReadOnlyList<string> IExample.SourceFiles => ["MermaidSequenceExample.cs", "MermaidViewer.cs", "sample-seq.mmd"];
    #endregion
}
