namespace Jumbee.Console.Tests;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

using ConsoleGUI.Api;
using ConsoleGUI.Data;
using ConsoleGUI.Space;

using Jumbee.Console;

using Xunit;

/// <summary>
/// <see cref="Control.Feed"/>: a repeating background timer that posts ticks onto the UI thread, cancellable via the
/// returned handle and auto-cancelled on <see cref="Control.Dispose"/>. Drives the real UI loop so posts actually drain.
/// </summary>
public class FeedTests
{
    public FeedTests() => UiTestHarness.EnsureStopped();

    private static Task StartUi() => UI.Start(
        new VerticalStackPanel(new TextLabel(TextLabelOrientation.Horizontal, "x")),
        40, 8, fps: 100, isAnsiTerminal: true, console: new StubConsole(40, 8), input: new NoInput());

    private static bool WaitUntil(Func<bool> cond, int ms)
    {
        var sw = Stopwatch.StartNew();
        while (!cond() && sw.ElapsedMilliseconds < ms) Thread.Sleep(5);
        return cond();
    }

    // After stopping a feed, ticks must stop advancing. Sleeps past any in-flight (already-posted) tick, then checks
    // the count is stable across a further window.
    private static void AssertStopped(Func<int> count)
    {
        Thread.Sleep(60);
        int settled = count();
        Thread.Sleep(90);
        Assert.Equal(settled, count());
    }

    [Fact]
    public void Feed_PostsTicks_AndCancelStops()
    {
        var run = StartUi();
        try
        {
            var c = new FeedControl();
            var cts = c.StartTickFeed(10);
            Assert.True(WaitUntil(() => c.Ticks >= 3, 2000), "feed should post ticks onto the UI thread");

            cts.Cancel();
            AssertStopped(() => c.Ticks);
        }
        finally { UI.Stop(); run.Wait(2000); }
    }

    [Fact]
    public void Dispose_CancelsLiveFeeds()
    {
        var run = StartUi();
        try
        {
            var c = new FeedControl();
            c.StartTickFeed(10);
            Assert.True(WaitUntil(() => c.Ticks >= 2, 2000));

            c.Dispose();                       // must cancel the still-running feed
            AssertStopped(() => c.Ticks);
        }
        finally { UI.Stop(); run.Wait(2000); }
    }

    [Fact]
    public void Feed_ProducerConsumer_ProducesOffThread_AppliesOnUiThread()
    {
        var run = StartUi();
        try
        {
            var c = new FeedControl();
            var cts = c.StartProducerFeed(10);
            Assert.True(WaitUntil(() => c.Applied >= 2, 2000), "the producer/consumer feed should apply results");
            cts.Cancel();

            Assert.False(c.ProduceOnUiThread, "produce must run on the background thread, not the UI thread");
            Assert.True(c.ApplyOnUiThread, "apply must run on the UI thread");
        }
        finally { UI.Stop(); run.Wait(2000); }
    }

    // The Feeds snapshot backs IActivatableExample's default OnDeactivated: a live example cancels every Feeds handle
    // to stop its feeds while hidden. Verify the snapshot lists live feeds and cancelling them stops the ticks.
    [Fact]
    public void Feeds_SnapshotListsLiveFeeds_AndCancellingThemStops()
    {
        var run = StartUi();
        try
        {
            var c = new FeedControl();
            c.StartTickFeed(10);
            c.StartTickFeed(10);
            Assert.Equal(2, c.LiveFeeds.Count);   // both registered on start
            Assert.True(WaitUntil(() => c.Ticks >= 2, 2000));

            foreach (var f in c.LiveFeeds) f.Cancel();   // exactly what IActivatableExample.OnDeactivated does
            AssertStopped(() => c.Ticks);
        }
        finally { UI.Stop(); run.Wait(2000); }
    }

    private sealed class FeedControl : Control
    {
        private int _ticks, _applied;
        public int Ticks => Volatile.Read(ref _ticks);
        public int Applied => Volatile.Read(ref _applied);
        public IReadOnlyList<CancellationTokenSource> LiveFeeds => Feeds;
        public volatile bool ProduceOnUiThread = true;   // start pessimistic; produce (off-thread) must flip it false
        public volatile bool ApplyOnUiThread;

        public CancellationTokenSource StartTickFeed(int ms) => Feed(() => Interlocked.Increment(ref _ticks), ms);

        public CancellationTokenSource StartProducerFeed(int ms) => Feed(
            produce: () => { ProduceOnUiThread = UI.CheckAccess(); return 1; },
            apply: _ => { ApplyOnUiThread = UI.CheckAccess(); Interlocked.Increment(ref _applied); },
            intervalMs: ms);

        protected override void Render() { }
    }

    private sealed class StubConsole(int w, int h) : IConsole
    {
        public Size Size { get; set; } = new Size(w, h);
        public bool KeyAvailable => false;
        public void Initialize() { }
        public void OnRefresh() { }
        public void Write(Position position, in Character character) { }
        public ConsoleKeyInfo ReadKey() => throw new NotSupportedException();
    }

    private sealed class NoInput : IInputSource
    {
        public bool TryRead(out TerminalInputEvent? evt) { evt = null; return false; }
    }
}
