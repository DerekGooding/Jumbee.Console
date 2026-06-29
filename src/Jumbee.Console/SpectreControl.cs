namespace Jumbee.Console;

using System;
using System.Collections.Generic;
using Spectre.Console.Rendering;

/// <summary>
/// Wraps an existing Spectre.Console <see cref="IRenderable"/> control for use with ConsoleGUI control and layout types. 
/// </summary>
/// <remarks>
/// Uses an <see cref="AnsiConsoleBuffer"/> to render the control to a buffer.
/// Public property setters and methods that change a control's visual state should call <see cref="Invalidate"/> to request a re-render on the next UI update tick.
/// Non-atomic changes to the wrapped content (e.g. mutating its collections) should go through <see cref="UpdateContent"/>,
/// which applies the change on the UI thread so it never races with rendering.
/// </remarks>
/// <typeparam name="T"></typeparam>
public class SpectreControl<T> : RenderableControl where T : IRenderable
{
    #region Constructors
    public SpectreControl(T content) : base()
    {
        _content = content;
    }
    #endregion
    
    #region Properties
    public T Content
    {
        get => _content;
        set 
        {
            _content = value;
            Invalidate();
        }
    }
    #endregion

    #region Methods
    /// <summary>
    /// Applies a mutation to the wrapped content on the UI thread (inline when already there, otherwise
    /// marshaled), so a non-atomic change never races with rendering. Replaces the former copy-on-write
    /// approach now that all mutation is serialized onto the UI thread.
    /// </summary>
    /// <param name="update">The update operation.</param>
    protected void UpdateContent(Action<T> update)
    {
        UI.Invoke(() =>
        {
            update(_content);
            Invalidate();
        });
    }

    protected override Measurement Measure(RenderOptions options, int maxWidth) => _content.Measure(options, maxWidth);

    protected override IEnumerable<Segment> Render(RenderOptions options, int maxWidth) => _content.Render(options, maxWidth);

    // Output is purely the wrapped content (Render never reads focus/hover), so skip re-rendering on interactive
    // state changes and reuse the cached buffer. Content changes still go through the Content setter / UpdateContent
    // (both call Invalidate).
    protected override bool RendersInteractiveState => false;
    
    #endregion

    #region Fields
    private T _content;
    #endregion
}
