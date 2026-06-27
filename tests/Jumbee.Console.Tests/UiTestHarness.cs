namespace Jumbee.Console.Tests;

using System.Diagnostics;
using System.Threading;

using Jumbee.Console;

/// <summary>
/// Shared setup for tests that start the real UI loop. The loop lives in process-wide static state and tests run
/// sequentially (parallelism is disabled assembly-wide), but they share that state — so a prior test whose UI is
/// still winding down can make the next <see cref="UI.Start"/> early-return on a stale <c>isRunning</c> (or race a
/// dispatcher thread that hasn't exited). Call <see cref="EnsureStopped"/> from a test class constructor (xUnit
/// builds a fresh instance per test) so each UI-starting test begins from a fully stopped state.
/// </summary>
internal static class UiTestHarness
{
    public static void EnsureStopped()
    {
        if (UI.IsRunning) UI.Stop();
        var sw = Stopwatch.StartNew();
        while ((UI.IsRunning || UI.Dispatcher.IsRunning) && sw.ElapsedMilliseconds < 2000)
            Thread.Sleep(5);
    }
}
