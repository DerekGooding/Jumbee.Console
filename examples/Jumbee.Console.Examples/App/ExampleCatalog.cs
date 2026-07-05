namespace Jumbee.Console.Examples;

using System.Collections.Generic;

/// <summary>
/// The explicit registry of examples, in display order. Explicit registration (rather than reflection) keeps
/// <c>PublishAot</c> happy and makes "add an example" a one-line change here plus one small class.
/// </summary>
public static class ExampleCatalog
{
    public static IReadOnlyList<IExample> All { get; } =
    [
        new WelcomeExample(),
        new ButtonExample(),
        new ListBoxExample(),
        new PlotExample(),
        new DialogExample(),
        new SplitPanelExample(),
        new SpectreTableExample(),
        new MultiTabEditorExample(),
    ];

    /// <summary>The example shown when the browser opens.</summary>
    public static IExample Default => All[0];
}
