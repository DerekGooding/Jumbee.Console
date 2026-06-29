namespace Jumbee.Console.Tests;

using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Jumbee.Console;

using Xunit;

public class DispatcherTests
{
    private static Dispatcher StartIdle(out Dispatcher d, int interval = 50)
    {
        d = new Dispatcher();
        d.Start(() => { }, interval);
        return d;
    }

    [Fact]
    public void CheckAccess_BeforeStart_ReturnsTrue()
    {
        var d = new Dispatcher();
        Assert.True(d.CheckAccess());   // no UI thread yet -> inline callers have access
    }

    [Fact]
    public void CheckAccess_FromOtherThread_WhileRunning_ReturnsFalse()
    {
        StartIdle(out var d);
        try
        {
            Assert.False(d.CheckAccess());   // the test thread is not the UI thread
        }
        finally { d.Stop(); }
    }

    [Fact]
    public void Post_RunsActionOnUiThread()
    {
        StartIdle(out var d);
        try
        {
            int ranThreadId = -1;
            var accessInside = false;
            using var done = new ManualResetEventSlim(false);

            d.Post(() =>
            {
                ranThreadId = Environment.CurrentManagedThreadId;
                accessInside = d.CheckAccess();
                done.Set();
            });

            Assert.True(done.Wait(2000), "Posted action did not run.");
            Assert.Equal(d.ThreadId, ranThreadId);
            Assert.NotEqual(Environment.CurrentManagedThreadId, ranThreadId);
            Assert.True(accessInside);
        }
        finally { d.Stop(); }
    }

    [Fact]
    public void Invoke_BlocksAndRunsOnUiThread()
    {
        StartIdle(out var d);
        try
        {
            var ranThreadId = -1;
            d.Invoke(() => ranThreadId = Environment.CurrentManagedThreadId);

            Assert.Equal(d.ThreadId, ranThreadId);
            Assert.NotEqual(Environment.CurrentManagedThreadId, ranThreadId);
        }
        finally { d.Stop(); }
    }

    [Fact]
    public void Invoke_PropagatesException()
    {
        StartIdle(out var d);
        try
        {
            var ex = Assert.Throws<InvalidOperationException>(
                () => d.Invoke(() => throw new InvalidOperationException("boom")));
            Assert.Equal("boom", ex.Message);
        }
        finally { d.Stop(); }
    }

    [Fact]
    public async Task InvokeAsync_Action_CompletesOnUiThread()
    {
        StartIdle(out var d);
        try
        {
            var ranThreadId = -1;
            await d.InvokeAsync(() => ranThreadId = Environment.CurrentManagedThreadId);
            Assert.Equal(d.ThreadId, ranThreadId);
        }
        finally { d.Stop(); }
    }

    [Fact]
    public async Task InvokeAsync_Func_ReturnsResult()
    {
        StartIdle(out var d);
        try
        {
            var result = await d.InvokeAsync(() => 21 + 21);
            Assert.Equal(42, result);
        }
        finally { d.Stop(); }
    }

    [Fact]
    public async Task InvokeAsync_PropagatesException()
    {
        StartIdle(out var d);
        try
        {
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => d.InvokeAsync(() => throw new InvalidOperationException("nope")));
        }
        finally { d.Stop(); }
    }

    [Fact]
    public void Post_PreservesFifoOrder()
    {
        StartIdle(out var d);
        try
        {
            var order = new ConcurrentQueue<int>();
            for (var i = 0; i < 50; i++)
            {
                var n = i;
                d.Post(() => order.Enqueue(n));
            }

            // Invoke acts as a barrier: by the time it runs, all earlier posts have drained (FIFO).
            d.Invoke(() => { });

            Assert.Equal(Enumerable.Range(0, 50), order.ToArray());
        }
        finally { d.Stop(); }
    }

    [Fact]
    public void Frame_FiresPeriodically_AndStopsAfterStop()
    {
        var frames = 0;
        var d = new Dispatcher();
        d.Start(() => Interlocked.Increment(ref frames), 20);

        Thread.Sleep(150);
        Assert.True(d.IsRunning);
        d.Stop();

        Assert.False(d.IsRunning);
        Assert.True(frames >= 3, $"Expected several frames, got {frames}.");

        // No further frames once stopped.
        var afterStop = Volatile.Read(ref frames);
        Thread.Sleep(80);
        Assert.Equal(afterStop, Volatile.Read(ref frames));
    }

    [Fact]
    public void SelfRepostingWork_DoesNotStarveFrames()
    {
        // Reproduces the terminal flood freeze: the output pump (DrainOutput) re-posts itself while the producer
        // keeps the queue fed, so a "drain until empty" frame loop never reaches the render. The frame must keep
        // firing because the per-frame drain is bounded to the work pending at frame start.
        var frames = 0;
        using var firstFrame = new ManualResetEventSlim(false);
        var d = new Dispatcher();
        d.Start(() => { Interlocked.Increment(ref frames); firstFrame.Set(); }, frameIntervalMs: 5);
        try
        {
            void Pump() { Thread.SpinWait(50); d.Post(Pump); }   // re-posts itself forever, like the flood pump
            d.Post(Pump);

            Assert.True(firstFrame.Wait(2000), "a frame must run even while self-reposting work floods the queue");
            var before = Volatile.Read(ref frames);
            Thread.Sleep(200);
            Assert.True(Volatile.Read(ref frames) > before + 2,
                $"frames must keep advancing under a self-reposting flood (before={before}, after={Volatile.Read(ref frames)})");
        }
        finally { d.Stop(); }
    }

    [Fact]
    public void Start_IsIdempotent_WhileRunning()
    {
        StartIdle(out var d);
        try
        {
            var firstThread = d.ThreadId;
            d.Start(() => { }, 50);     // second start should be a no-op
            Assert.Equal(firstThread, d.ThreadId);
        }
        finally { d.Stop(); }
    }

    [Fact]
    public void UiThread_HasSynchronizationContext_ThatMarshalsToUiThread()
    {
        StartIdle(out var d);
        try
        {
            SynchronizationContext? ctx = null;
            d.Invoke(() => ctx = SynchronizationContext.Current);
            Assert.NotNull(ctx);

            // Posting through that context must run on the UI thread.
            var ranThreadId = -1;
            using var done = new ManualResetEventSlim(false);
            ctx!.Post(_ => { ranThreadId = Environment.CurrentManagedThreadId; done.Set(); }, null);

            Assert.True(done.Wait(2000), "Context post did not run.");
            Assert.Equal(d.ThreadId, ranThreadId);
        }
        finally { d.Stop(); }
    }

    [Fact]
    public void AwaitContinuation_OnUiThread_ResumesOnUiThread()
    {
        StartIdle(out var d);
        try
        {
            var continuationThreadId = -1;
            using var done = new ManualResetEventSlim(false);

            // Runs on the UI thread; the await captures the dispatcher's SynchronizationContext, so the
            // continuation must resume on the UI thread.
            d.Post(async () =>
            {
                await Task.Delay(20);
                continuationThreadId = Environment.CurrentManagedThreadId;
                done.Set();
            });

            Assert.True(done.Wait(3000), "Continuation did not run.");
            Assert.Equal(d.ThreadId, continuationThreadId);
        }
        finally { d.Stop(); }
    }

    [Fact]
    public async Task InvokeAsync_FuncTask_AwaitsInnerWork_OnUiThread()
    {
        StartIdle(out var d);
        try
        {
            var completed = false;
            var continuationThreadId = -1;

            // Binds to InvokeAsync(Func<Task>) (preferred over Action for async lambdas), so the returned
            // task only completes after the inner await finishes.
            await d.InvokeAsync(async () =>
            {
                await Task.Delay(30);
                continuationThreadId = Environment.CurrentManagedThreadId;
                completed = true;
            });

            Assert.True(completed, "InvokeAsync should await the inner work, not complete at the first await.");
            Assert.Equal(d.ThreadId, continuationThreadId);
        }
        finally { d.Stop(); }
    }

    [Fact]
    public async Task InvokeAsync_FuncTask_PropagatesExceptionAfterAwait()
    {
        StartIdle(out var d);
        try
        {
            await Assert.ThrowsAsync<InvalidOperationException>(() => d.InvokeAsync(async () =>
            {
                await Task.Delay(10);
                throw new InvalidOperationException("after await");
            }));
        }
        finally { d.Stop(); }
    }

    [Fact]
    public async Task InvokeAsync_FuncTaskT_ReturnsUnwrappedResult()
    {
        StartIdle(out var d);
        try
        {
            var result = await d.InvokeAsync(async () =>
            {
                await Task.Delay(10);
                return 123;
            });

            Assert.Equal(123, result);
        }
        finally { d.Stop(); }
    }
}
