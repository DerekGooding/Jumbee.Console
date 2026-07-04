namespace Jumbee.Console.Examples;

using System.Collections.Generic;

/// <summary>
/// Marks a control (or layout) as a browsable example: it <em>is</em> the live content shown in the middle pane
/// (<c>IExample : IFocusable</c>), plus the metadata the tree/header use and the source files the viewer shows. So an
/// example is just a normal control that also carries a little metadata — e.g. <c>class ListBoxExample : ListBox,
/// IExample</c>. <see cref="SourceFiles"/> defaults to the type's own <c>.cs</c> file.
/// </summary>
public interface IExample : IFocusable
{
    /// <summary>Tree grouping, e.g. "Flexibility" or "Controls".</summary>
    string Category { get; }

    /// <summary>Tree label and middle-pane frame title.</summary>
    string Title { get; }

    /// <summary>One-line summary shown above the demo.</summary>
    string Description { get; }

    /// <summary>Source file names shown read-only in the right pane; resolved against the embedded resources by
    /// <see cref="SourceLoader"/>. Defaults to this type's own <c>.cs</c> file.</summary>
    IReadOnlyList<string> SourceFiles => [GetType().Name + ".cs"];
}
