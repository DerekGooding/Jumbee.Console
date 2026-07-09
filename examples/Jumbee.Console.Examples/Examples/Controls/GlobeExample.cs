namespace Jumbee.Console.Examples;

using System;
using System.Threading;
using System.Threading.Tasks;

using Jumbee.Console;

/// <summary>
/// A live, self-rotating ASCII Earth — the ray-traced <see cref="Globe"/> control spun a few dozen times a second
/// from a background timer. Each tick advances the globe's rotation (and counter-turns the camera) and requests a
/// redraw; the day/night terminator sweeps across as it turns. Demonstrates driving a <b>compute-heavy live control</b>
/// off a posted feed (<see cref="UI.Post"/>), the same pattern as the dashboards — the whole sphere is re-ray-traced
/// each frame yet stays well under the frame budget.
/// </summary>
public sealed class GlobeExample : Globe, IExample, IActivatable
{
    #region Live feed
    // Spin while shown; stop when hidden (called by ExampleHost). A wall-clock timer POSTS each step onto the UI
    // thread so its redraw is requested at frame start and composited by the frame loop (see LiveDashboardExample).
    public void OnActivated()
    {
        _cts = new CancellationTokenSource();
        var ct = _cts.Token;
        _ = Task.Run(async () =>
        {
            while (!ct.IsCancellationRequested)
            {
                try { await Task.Delay(33, ct); }
                catch (TaskCanceledException) { break; }
                UI.Post(() => Spin(0.03));
            }
        });
    }

    public void OnDeactivated()
    {
        _cts?.Cancel();
        _cts = null;
    }
    #endregion

    #region IExample
    public bool FillsPane => true;
    public string Category => "Flexibility";
    public string Title => "Rotating Globe";
    public string Description =>
        "A ray-traced ASCII Earth, re-rendered every frame — the day/night terminator sweeps as it spins.";
    #endregion

    #region Fields
    private CancellationTokenSource? _cts;
    #endregion
}
