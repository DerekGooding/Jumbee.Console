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
}
