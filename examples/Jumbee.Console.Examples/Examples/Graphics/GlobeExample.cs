namespace Jumbee.Console.Examples;

using System.Collections.Generic;
using System.Threading;

using Jumbee.Console;

/// <summary>
/// A self-rotating ASCII Earth: the ray-traced <see cref="Globe"/> spun by a <see cref="Control.Feed"/> — a
/// compute-heavy live control re-rendered every frame, comfortably under the frame budget.
/// </summary>
public sealed class GlobeExample : Globe, IActivatableExample
{
    #region IExample
    // Spin while shown; the base IActivatableExample.OnDeactivated cancels the feed (registered in Feeds) when hidden.
    void IActivatableExample.OnActivated() => Feed(() => Spin(0.03), 33);

    IReadOnlyList<CancellationTokenSource> IActivatableExample.FeedTasks => Feeds;

    string IExample.Category => "Graphics";
    string IExample.Title => "Rotating Globe";
    string IExample.Description =>
        "A ray-traced ASCII Earth, re-rendered every frame — the day/night terminator sweeps as it spins.";
    #endregion
}
