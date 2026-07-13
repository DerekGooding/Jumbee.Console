namespace Jumbee.Console.Examples;

using System.Collections.Generic;

using Jumbee.Console.Documents;

/// <summary>
/// The <see cref="MermaidViewer"/> rendering a Mermaid <c>erDiagram</c> — entity tables (typed attributes with
/// PK/FK/UK keys) joined by crow's-foot cardinality relationships; scroll with arrows/PgUp/PgDn/wheel.
/// </summary>
public sealed class MermaidErExample : MermaidViewer, IExample
{
    public MermaidErExample() : base(SourceLoader.Read("sample-er.mmd")) { }

    #region IExample
    string IExample.Category => "Editors and Viewers";
    string IExample.Title => "Mermaid ER Diagram";
    string IExample.Description =>
        "Renders a Mermaid entity-relationship diagram — attribute tables joined by crow's-foot cardinalities — as scrollable box-drawing graphics.";
    IReadOnlyList<string> IExample.SourceFiles => ["MermaidErExample.cs", "MermaidViewer.cs", "sample-er.mmd"];
    #endregion
}
