namespace Jumbee.Console.Examples;

using System.Threading;

using Jumbee.Console;

/// <summary>
/// A live, self-rotating ASCII Earth — the ray-traced <see cref="Globe"/> control spun a few dozen times a second by
/// a <see cref="Control.Feed"/>. Each tick advances the globe's rotation and requests a redraw; the texture scrolls
/// under the fixed light so the day/night terminator sweeps across as it turns. Demonstrates driving a
/// <b>compute-heavy live control</b> off a posted feed — the whole sphere is re-ray-traced each frame yet stays well
/// under the frame budget.
/// </summary>
public sealed class GlobeExample : Globe, IExample, IActivatable
{
    #region Live feed
    // Spin while shown; stop when hidden (called by ExampleHost). Control.Feed runs the timer and posts each spin
    // onto the UI thread; cancelling the returned handle stops it (Dispose would too, as a backstop).
    public void OnActivated() => _feed = Feed(() => Spin(0.03), 33);

    public void OnDeactivated()
    {
        _feed?.Cancel();
        _feed = null;
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
    private CancellationTokenSource? _feed;
    #endregion
}
