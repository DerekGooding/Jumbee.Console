namespace Jumbee.Console.Examples;

using System.Collections.Generic;

/// <summary>
/// One entry in the example browser: a live control for the middle pane, plus the metadata the tree/header use and
/// the source files the right-pane viewer shows. Keep <see cref="Build"/> pure — construct and return a control; do
/// not start the UI or show modals from it.
/// </summary>
public interface IExample
{
    /// <summary>Tree grouping, e.g. "Flexibility" or "Controls".</summary>
    string Category { get; }

    /// <summary>Tree label and middle-pane frame title.</summary>
    string Title { get; }

    /// <summary>One-paragraph summary shown to skim-readers (not just those who read the source).</summary>
    string Description { get; }

    /// <summary>Builds the live content shown in the middle pane — a control or a layout (both are
    /// <see cref="IFocusable"/>).</summary>
    IFocusable Build();

    /// <summary>Source file names (e.g. "ButtonExample.cs") shown read-only in the right pane; resolved against the
    /// embedded resources by <see cref="SourceLoader"/>.</summary>
    IReadOnlyList<string> SourceFiles { get; }
}

/// <summary>Base class with sensible defaults: the source file defaults to this type's own <c>.cs</c> file, so a
/// typical example is a single small class — itself a demonstration of the library's ease of use.</summary>
public abstract class ExampleBase : IExample
{
    public abstract string Category { get; }
    public abstract string Title { get; }
    public abstract string Description { get; }
    public abstract IFocusable Build();
    public virtual IReadOnlyList<string> SourceFiles => [GetType().Name + ".cs"];
}
