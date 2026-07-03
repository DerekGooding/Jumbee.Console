namespace Jumbee.Console.Tests;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

using Jumbee.Console;

using Xunit;

public class ProcessMetricsTests
{
    [Fact]
    public void Rates_AreZero_BeforeTwoSamples()
    {
        var m = new ProcessMetrics(windowMs: 1000);
        Assert.Equal(0, m.CpuUsagePercent);
        Assert.Equal(0, m.AllocatedBytesPerSecond);
        Assert.Equal(0, m.GcPausePercent);

        m.Sample();   // a single sample is still not a window
        Assert.Equal(0, m.AllocatedBytesPerSecond);
    }

    [Fact]
    public void Gauges_ReadLive_AndAreSane()
    {
        var m = new ProcessMetrics();
        Assert.True(m.WorkingSetBytes > 0);
        Assert.True(m.ManagedHeapBytes > 0);
        Assert.True(m.ThreadPoolThreadCount > 0);
        Assert.True(m.ThreadPoolQueueLength >= 0);
        Assert.True(m.Gen0Collections >= 0);
    }

    [Fact]
    public void CpuSupported_OnDesktopOs()
        => Assert.True(new ProcessMetrics().CpuSupported);   // the test host is Windows/Linux/macOS

    [Fact]
    public void Allocation_BetweenSamples_ShowsAsPerFrameAndRate()
    {
        var m = new ProcessMetrics(windowMs: 1000);
        m.Sample();

        var sink = new List<byte[]>();
        for (var i = 0; i < 64; i++) sink.Add(new byte[65536]);   // ~4 MB
        Thread.Sleep(5);                                          // let wall time advance for the rate
        m.Sample();

        Assert.True(m.AllocatedBytesPerFrame > 1_000_000, $"per-frame={m.AllocatedBytesPerFrame}");
        Assert.True(m.AllocatedBytesPerSecond > 0, $"per-second={m.AllocatedBytesPerSecond}");
        Assert.True(m.PeakAllocatedBytesPerFrame >= m.AllocatedBytesPerFrame);
        GC.KeepAlive(sink);
    }

    [Fact]
    public void CpuBurn_BetweenSamples_ShowsNonZeroCpu()
    {
        var m = new ProcessMetrics(windowMs: 1000);
        if (!m.CpuSupported) return;

        m.Sample();
        var sw = Stopwatch.StartNew();
        double x = 0;
        while (sw.ElapsedMilliseconds < 60) x += Math.Sqrt(sw.ElapsedTicks + 1);   // burn a core
        m.Sample();

        Assert.True(m.CpuUsagePercent > 0, $"cpu={m.CpuUsagePercent}");
        GC.KeepAlive(x);
    }

    [Fact]
    public void FirstChanceExceptions_BetweenSamples_ShowAsRate()
    {
        using var m = new ProcessMetrics(windowMs: 1000);
        m.Start();   // subscribes to FirstChanceException and takes the baseline sample

        for (var i = 0; i < 5; i++)
        {
            try { throw new InvalidOperationException("boom"); }
            catch { /* first-chance still fires */ }
        }
        Thread.Sleep(5);
        m.Sample();

        Assert.True(m.ExceptionsPerSecond > 0, $"exceptions/s={m.ExceptionsPerSecond}");
    }
}
