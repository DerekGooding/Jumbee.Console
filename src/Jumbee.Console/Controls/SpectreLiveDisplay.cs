namespace Jumbee.Console;

using System;
using System.Threading;
using System.Threading.Tasks;

using Spectre.Console;
using Spectre.Console.Rendering;

/// <summary>
/// A Spectre.Console LiveDisplay widget.
/// </summary>
public class SpectreLiveDisplay : Control
{
    #region Constructors
    public SpectreLiveDisplay(IRenderable target)
    {
        // LiveDisplay refreshes the buffer from a background thread; marshal those writes onto the UI thread.
        ansiConsole.marshal = true;
        this.target = target;
        display = ansiConsole.Live(target);
    }
    #endregion

    #region Fields
    protected readonly LiveDisplay display;
    protected IRenderable target;
    #endregion

    #region Methods
    public Task Start(Action<LiveDisplayContext> action) => Task.Run(() => display.Start(action));

    public Task StartAsync(Func<LiveDisplayContext, Task> action) => display.StartAsync(action);

    public Task<T> StartAsync<T>(Func<LiveDisplayContext, Task<T>> action) => display.StartAsync(action);

    // LiveDisplay will update console buffer
    protected override void Render() {}

    // Control is assumed to always require painting
    protected override void Validate() {}
    #endregion

}
