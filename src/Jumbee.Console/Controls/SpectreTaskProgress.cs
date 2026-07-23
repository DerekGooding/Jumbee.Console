namespace Jumbee.Console;

using Spectre.Console;
using System;
using System.Threading.Tasks;

/// <summary>
/// A Spectre.Console Progress widget.
/// </summary>
public class SpectreTaskProgress : Control
{
    #region Constructors

    /// <summary>Initializes a new <see cref="SpectreTaskProgress"/> whose progress refreshes onto the UI thread.</summary>
    public SpectreTaskProgress()
    {
        Focusable = false;   // a passive progress display: never a focus/tab target
        // Progress auto-refreshes the buffer from a background thread; marshal those writes onto the UI thread.
        ansiConsole.marshal = true;
        progress = new Progress(ansiConsole);
    }

    #endregion Constructors

    /// <summary><see langword="false"/>: a passive progress display that takes no keyboard input.</summary>
    public override bool HandlesInput => false;

    #region Fields

    /// <summary>The wrapped Spectre <see cref="Progress"/> driving the tasks and refresh loop.</summary>
    protected readonly Progress progress;

    #endregion Fields

    #region Methods

    /// <summary>Runs <paramref name="action"/> on a background thread with a progress context to drive the tasks.</summary>
    public Task Start(Action<ProgressContext> action) => Task.Run(() => progress.Start(action));

    /// <summary>Runs the asynchronous <paramref name="action"/> with a progress context to drive the tasks.</summary>
    public Task StartAsync(Func<ProgressContext, Task> action) => progress.StartAsync(action);

    /// <summary>Runs the asynchronous <paramref name="action"/> with a progress context and returns its result.</summary>
    public Task<T> StartAsync<T>(Func<ProgressContext, Task<T>> action) => progress.StartAsync(action);

    // Returns this wrapper (not the inner Spectre Progress) so a fluent .Start(...) resolves to the wrapper's
    // non-blocking Task.Run overload rather than Spectre's blocking Progress.Start, which would block the caller.
    /// <summary>Adds progress <paramref name="columns"/> (bar, percentage, spinner, etc.) and returns this wrapper for chaining.</summary>
    public SpectreTaskProgress AddColumns(params ProgressColumn[] columns)
    {
        progress.Columns(columns);
        return this;
    }

    // Progress control will update console buffer
    /// <summary>No-op: the wrapped <see cref="Progress"/> writes to the buffer from its own refresh loop.</summary>
    protected override void Render()
    { }

    // Control is assumed to always require painting
    /// <summary>No-op: the progress display is treated as always needing a repaint.</summary>
    protected override void Validate()
    { }

    #endregion Methods
}