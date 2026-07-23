using Spectre.Console.Rendering;

namespace Jumbee.Console;

/// <summary>
/// Wraps an existing Spectre.Console <see cref="IRenderable"/> control for use with ConsoleGUI control and layout types.
/// </summary>
/// <remarks>
/// Uses an <see cref="AnsiConsoleBuffer"/> to render the control to a buffer.
/// Public property setters and methods that change a control's visual state should call <see cref="Control.Invalidate()"/> to request a re-render on the next UI update tick.
/// Non-atomic changes to the wrapped content (e.g. mutating its collections) should go through <see cref="UpdateContent"/>,
/// which applies the change on the UI thread so it never races with rendering.
/// </remarks>
/// <typeparam name="T"></typeparam>
/// <remarks>Initializes a new <see cref="SpectreControl{T}"/> wrapping the given Spectre.Console <paramref name="content"/>.</remarks>
public class SpectreControl<T>(T content) : RenderableControl() where T : IRenderable
{
    #region Properties

    /// <summary>The wrapped Spectre.Console renderable; setting it requests a re-render.</summary>
    public T Content
    {
        get;
        set
        {
            field = value;
            Invalidate();
        }
    } = content;

    #endregion Properties

    #region Methods

    /// <summary>
    /// Applies a mutation to the wrapped content on the UI thread (inline when already there, otherwise
    /// marshaled), so a non-atomic change never races with rendering.
    /// </summary>
    /// <remarks>Replaces the former copy-on-write approach now that all mutation is serialized onto the UI thread.</remarks>
    /// <param name="update">The update operation.</param>
    protected void UpdateContent(Action<T> update) => UI.Invoke(() =>
                                                           {
                                                               update(Content);
                                                               Invalidate();
                                                           });

    /// <summary>Measures the wrapped content.</summary>
    protected override Measurement Measure(RenderOptions options, int maxWidth) => Content.Measure(options, maxWidth);

    /// <summary>Renders the wrapped content to segments.</summary>
    protected override IEnumerable<Segment> Render(RenderOptions options, int maxWidth) => Content.Render(options, maxWidth);

    // Output is purely the wrapped content (Render never reads focus/hover), so skip re-rendering on interactive
    // state changes and reuse the cached buffer. Content changes still go through the Content setter / UpdateContent
    // (both call Invalidate).
    /// <summary>Always <see langword="false"/> — the wrapped content's output does not depend on interactive state, so focus/hover changes reuse the cached buffer.</summary>
    protected override bool RendersInteractiveState => false;

    #endregion Methods
}