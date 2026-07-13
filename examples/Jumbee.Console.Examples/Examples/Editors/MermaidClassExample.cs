namespace Jumbee.Console.Examples;

using System.Collections.Generic;

using Jumbee.Console.Documents;

/// <summary>
/// The <see cref="MermaidViewer"/> rendering a Mermaid <c>classDiagram</c> — three-compartment UML boxes (name +
/// annotation, attributes, methods) with inheritance/aggregation relationship markers; scroll with arrows/PgUp/PgDn/wheel.
/// </summary>
public sealed class MermaidClassExample : MermaidViewer, IExample
{
    public MermaidClassExample() : base(SourceLoader.Read("sample-class.mmd")) { }

    #region IExample
    string IExample.Category => "Editors and Viewers";
    string IExample.Title => "Mermaid Class Diagram";
    string IExample.Description =>
        "Renders a Mermaid UML class diagram — compartmented classes with typed members and relationship markers — as scrollable box-drawing graphics.";
    IReadOnlyList<string> IExample.SourceFiles => ["MermaidClassExample.cs", "MermaidViewer.cs", "sample-class.mmd"];
    #endregion
}
