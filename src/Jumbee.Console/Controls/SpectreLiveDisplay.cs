using Spectre.Console;
using Spectre.Console.Rendering;

namespace Jumbee.Console;

/// <summary>
/// A Spectre.Console LiveDisplay widget.
/// </summary>
public class SpectreLiveDisplay : Control
{
    #region Constructors

    /// <summary>Initializes a new <see cref="SpectreLiveDisplay"/> around the <paramref name="target"/> renderable it live-refreshes.</summary>
    public SpectreLiveDisplay(IRenderable target)
    {
        Focusable = false;   // a passive live display: never a focus/tab target
        // LiveDisplay refreshes the buffer from a background thread; marshal those writes onto the UI thread.
        ansiConsole.marshal = true;
        this.target = target;
        display = ansiConsole.Live(target);
    }

    #endregion Constructors

    #region Fields

    /// <summary>The wrapped Spectre <see cref="LiveDisplay"/> driving the refresh loop.</summary>
    protected readonly LiveDisplay display;

    /// <summary>The renderable being live-displayed.</summary>
    protected IRenderable target;

    #endregion Fields

    #region Methods

    /// <summary>Runs <paramref name="action"/> on a background thread with a live-refreshing display context.</summary>
    public Task Start(Action<LiveDisplayContext> action) => Task.Run(() => display.Start(action));

    /// <summary>Runs the asynchronous <paramref name="action"/> with a live-refreshing display context.</summary>
    public Task StartAsync(Func<LiveDisplayContext, Task> action) => display.StartAsync(action);

    /// <summary>Runs the asynchronous <paramref name="action"/> with a live-refreshing display context and returns its result.</summary>
    public Task<T> StartAsync<T>(Func<LiveDisplayContext, Task<T>> action) => display.StartAsync(action);

    // LiveDisplay will update console buffer
    /// <summary>No-op: the wrapped <see cref="LiveDisplay"/> writes to the buffer from its own refresh loop.</summary>
    protected override void Render()
    { }

    // Control is assumed to always require painting
    /// <summary>No-op: the live display is treated as always needing a repaint.</summary>
    protected override void Validate()
    { }

    #endregion Methods
}