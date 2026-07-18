namespace Jumbee.Console.Examples;

using System.Collections.Generic;

/// <summary>
/// A read-only <see cref="MarkdownViewer"/> rendering one of the project's own design docs — headings, lists,
/// syntax-highlighted code and box-drawn tables — scrollable with the arrows, PgUp/PgDn, Home/End and the wheel.
/// </summary>
public sealed class MarkdownViewerExample : MarkdownViewer, IExample
{
    public MarkdownViewerExample() : base(SourceLoader.Read("Input.md")) { }

    #region IExample
    string IExample.Category => "Viewers";
    string IExample.Title => "Markdown Viewer";
    string IExample.Description =>
        "Renders Markdown — headings, lists, syntax-highlighted code and box-drawn tables — as a read-only, scrollable view.";
    // Show the example and the control, plus the raw Markdown it renders (the embedded Input.md design doc).
    IReadOnlyList<string> IExample.SourceFiles => ["MarkdownViewerExample.cs", "MarkdownViewer.cs", "Input.md"];
    #endregion
}
