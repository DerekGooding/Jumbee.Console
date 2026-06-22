namespace Jumbee.Console;

using System;
using System.Threading;
using System.Threading.Tasks;

using Spectre.Console;
using Spectre.Console.Rendering;

/// <summary>
/// A Spectre.Console Progress widget.
/// </summary>
public class SpectreTaskProgress : Control
{
    #region Constructors
    public SpectreTaskProgress()
    {
        // Progress auto-refreshes the buffer from a background thread; marshal those writes onto the UI thread.
        ansiConsole.marshal = true;
        progress = new Progress(ansiConsole);
    }
    #endregion

    public override bool HandlesInput => false;
    #region Fields
    protected readonly Progress progress;
    #endregion

    #region Methods
    public Task Start(Action<ProgressContext> action) => Task.Run(() => progress.Start(action));

    public Task StartAsync(Func<ProgressContext, Task> action) => progress.StartAsync(action);

    public Task<T> StartAsync<T>(Func<ProgressContext, Task<T>> action) => progress.StartAsync(action);

    // Returns this wrapper (not the inner Spectre Progress) so a fluent .Start(...) resolves to the wrapper's
    // non-blocking Task.Run overload rather than Spectre's blocking Progress.Start, which would block the caller.
    public SpectreTaskProgress AddColumns(params ProgressColumn[] columns)
    {
        progress.Columns(columns);
        return this;
    }
       
    // Progress control will update console buffer
    protected override void Render() {}

    // Control is assumed to always require painting
    protected override void Validate() {}
    #endregion

}
