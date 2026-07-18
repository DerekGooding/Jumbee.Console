namespace Jumbee.Console.Examples;

using System.Collections.Generic;

using Jumbee.Console.Documents;

/// <summary>
/// A read-only <see cref="AsciiDocViewer"/> rendering a sample AsciiDoc doc — sections, inline formatting, lists and
/// checklists, admonitions, source blocks, an embedded <c>[source,mermaid]</c> diagram, tables and quotes — scrollable
/// with the arrows, PgUp/PgDn, Home/End and the wheel.
/// </summary>
public sealed class AsciiDocViewerExample : AsciiDocViewer, IExample
{
    public AsciiDocViewerExample() : base(SourceLoader.Read("sample.adoc")) { }

    #region IExample
    string IExample.Category => "Viewers";
    string IExample.Title => "AsciiDoc Viewer";
    string IExample.Description =>
        "Renders AsciiDoc — sections, admonitions, source blocks, embedded mermaid diagrams, tables and quotes — as a read-only, scrollable view (via AdocNet).";
    // Show the example and the control, plus the raw AsciiDoc it renders.
    IReadOnlyList<string> IExample.SourceFiles => ["AsciiDocViewerExample.cs", "AsciiDocViewer.cs", "sample.adoc"];
    #endregion
}
