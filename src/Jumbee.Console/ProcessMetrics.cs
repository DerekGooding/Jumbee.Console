namespace Jumbee.Console;

using System;
using System.Diagnostics;
using System.Runtime.ExceptionServices;
using System.Threading;

/// <summary>
/// Collects live process/runtime performance metrics for the perf HUD by reading the same runtime APIs the
/// <c>System.Runtime</c> meter wraps (<see cref="GC"/>, <see cref="Environment"/>, <see cref="ThreadPool"/>,
/// <see cref="Monitor"/>) directly — no <c>MeterListener</c>, so there is nothing to sample-schedule and no
/// observable-instrument staleness.
/// </summary>
/// <remarks>
/// <para><see cref="Sample"/> is called once per frame from the UI loop; it is allocation-free so it does not
/// pollute the per-frame allocation metric. Cumulative counters (CPU time, allocated bytes, GC pause time,
/// exceptions) are differenced over a rolling ~1&#160;s window to yield rates; gauges (working set, heap, thread
/// pool) are read live on demand.</para>
/// <para>All state is written by <see cref="Sample"/> and read by the getters on the UI thread; only the
/// first-chance exception count is cross-thread (it uses <see cref="Interlocked"/>).</para>
/// </remarks>
public sealed class ProcessMetrics : IDisposable
{
    #region Constructors
    /// <param name="capacity">Number of per-frame snapshots retained (must cover <paramref name="windowMs"/> at the
    /// frame rate; the default covers ~6&#160;s at a 100&#160;ms tick).</param>
    /// <param name="windowMs">Rolling window over which rates (CPU%, alloc/s, GC-pause%, exceptions/s) are measured.</param>
    public ProcessMetrics(int capacity = 128, int windowMs = 1000)
    {
        _samples = new Snapshot[Math.Max(2, capacity)];
        _frameAlloc = new long[Math.Max(2, capacity)];
        _windowMs = windowMs;
        // dotnet.process.cpu.time is platform-guarded in the runtime; Environment.CpuUsage throws
        // PlatformNotSupportedException on Browser/WASI/tvOS/iOS(non-Catalyst). Mirror that guard.
        _cpuSupported = !OperatingSystem.IsBrowser() && !OperatingSystem.IsWasi()
            && !OperatingSystem.IsTvOS() && !(OperatingSystem.IsIOS() && !OperatingSystem.IsMacCatalyst());
    }
    #endregion

    #region Properties
    /// <summary><see langword="true"/> if per-process CPU time is available on this OS (see the constructor guard).</summary>
    public bool CpuSupported => _cpuSupported;

    /// <summary>Process CPU usage over the rolling window as a percentage of a single core (user + kernel). A busy
    /// UI thread reads ~100%; multi-threaded work can exceed 100%. 0 when CPU time is unavailable.</summary>
    public double CpuUsagePercent
    {
        get
        {
            if (!_cpuSupported || !TryWindow(out var o, out var n, out var ms)) return 0;
            return Math.Max(0, (n.CpuMs - o.CpuMs) / ms * 100.0);
        }
    }

    /// <summary>Physical memory mapped to the process (<see cref="Environment.WorkingSet"/>), in bytes.</summary>
    public long WorkingSetBytes => Environment.WorkingSet;

    /// <summary>Current managed heap size (<see cref="GC.GetTotalMemory(bool)"/>), in bytes.</summary>
    public long ManagedHeapBytes => GC.GetTotalMemory(forceFullCollection: false);

    /// <summary>Mean bytes allocated on the managed heap per frame, over the retained snapshots. The headline
    /// retained-mode number: an idle UI should allocate ~nothing per frame.</summary>
    public double AllocatedBytesPerFrame
    {
        get
        {
            if (_faCount == 0) return 0;
            long total = 0;
            for (int i = 0; i < _faCount; i++) total += _frameAlloc[i];
            return (double)total / _faCount;
        }
    }

    /// <summary>Peak bytes allocated in a single frame over the retained snapshots.</summary>
    public long PeakAllocatedBytesPerFrame
    {
        get
        {
            long peak = 0;
            for (int i = 0; i < _faCount; i++) if (_frameAlloc[i] > peak) peak = _frameAlloc[i];
            return peak;
        }
    }

    /// <summary>Managed heap allocation rate over the rolling window, in bytes per second.</summary>
    public double AllocatedBytesPerSecond
    {
        get => TryWindow(out var o, out var n, out var ms) ? Math.Max(0, (n.Allocated - o.Allocated) / (ms / 1000.0)) : 0;
    }

    /// <summary>Fraction of wall time (0..100) the runtime spent paused for GC over the rolling window — the
    /// clearest signal that GC is hitching the UI.</summary>
    public double GcPausePercent
    {
        get => TryWindow(out var o, out var n, out var ms) ? Math.Clamp((n.GcPauseMs - o.GcPauseMs) / ms * 100.0, 0, 100) : 0;
    }

    /// <summary>First-chance managed exceptions per second over the rolling window (thrown, caught or not). A
    /// steady non-zero value flags swallowed-exception churn.</summary>
    public double ExceptionsPerSecond
    {
        get => TryWindow(out var o, out var n, out var ms) ? Math.Max(0, (n.Exceptions - o.Exceptions) / (ms / 1000.0)) : 0;
    }

    /// <summary>Total monitor lock contentions since process start (<see cref="Monitor.LockContentionCount"/>) — the
    /// no-lock-design dagger; stays at 0 for the single-threaded UI model.</summary>
    public long LockContentions => Monitor.LockContentionCount;

    /// <summary>Garbage collections since process start for gen 0 / 1 / 2.</summary>
    public int Gen0Collections => GC.CollectionCount(0);
    public int Gen1Collections => GC.CollectionCount(1);
    public int Gen2Collections => GC.CollectionCount(2);

    /// <summary>Thread pool worker threads that currently exist.</summary>
    public int ThreadPoolThreadCount => ThreadPool.ThreadCount;

    /// <summary>Work items currently queued to the thread pool.</summary>
    public long ThreadPoolQueueLength => ThreadPool.PendingWorkItemCount;
    #endregion

    #region Methods
    /// <summary>Begins collection: subscribes to first-chance exceptions and takes a baseline sample.</summary>
    public void Start()
    {
        if (_started) return;
        _started = true;
        AppDomain.CurrentDomain.FirstChanceException += OnFirstChanceException;
        Sample();
    }

    /// <summary>Stops collection and unsubscribes.</summary>
    public void Stop()
    {
        if (!_started) return;
        _started = false;
        AppDomain.CurrentDomain.FirstChanceException -= OnFirstChanceException;
    }

    /// <summary>Records one per-frame snapshot of the cumulative counters. Allocation-free (so it does not distort
    /// <see cref="AllocatedBytesPerFrame"/>); call once per frame on the UI thread.</summary>
    public void Sample()
    {
        long timestamp = Stopwatch.GetTimestamp();
        long allocated = GC.GetTotalAllocatedBytes();                        // approximate, cheap, no allocation
        double cpuMs = _cpuSupported ? Environment.CpuUsage.TotalTime.TotalMilliseconds : 0;
        double gcPauseMs = GC.GetTotalPauseDuration().TotalMilliseconds;
        long exceptions = Interlocked.Read(ref _exceptions);

        if (_count > 0)
        {
            long delta = allocated - Last().Allocated;
            PushFrameAlloc(delta < 0 ? 0 : delta);
        }
        Push(new Snapshot(timestamp, cpuMs, allocated, gcPauseMs, exceptions));
    }

    public void Dispose()
    {
        Stop();
        GC.SuppressFinalize(this);
    }

    private void OnFirstChanceException(object? sender, FirstChanceExceptionEventArgs e) => Interlocked.Increment(ref _exceptions);

    // The newest snapshot and the oldest snapshot still within the rolling window, plus the wall time between them.
    // False until there are two snapshots spanning a non-zero interval.
    private bool TryWindow(out Snapshot oldest, out Snapshot newest, out double elapsedMs)
    {
        oldest = default;
        newest = default;
        elapsedMs = 0;
        if (_count < 2) return false;

        newest = Last();
        oldest = newest;
        double windowTicks = _windowMs * (Stopwatch.Frequency / 1000.0);
        for (int i = _count - 2; i >= 0; i--)
        {
            var s = At(i);
            if (newest.Timestamp - s.Timestamp <= windowTicks) oldest = s;
            else break;
        }
        elapsedMs = (newest.Timestamp - oldest.Timestamp) * 1000.0 / Stopwatch.Frequency;
        return elapsedMs > 0;
    }

    private void Push(in Snapshot s)
    {
        int idx = (_start + _count) % _samples.Length;
        _samples[idx] = s;
        if (_count < _samples.Length) _count++;
        else _start = (_start + 1) % _samples.Length;
    }

    private Snapshot At(int i) => _samples[(_start + i) % _samples.Length];
    private Snapshot Last() => At(_count - 1);

    private void PushFrameAlloc(long bytes)
    {
        int idx = (_faStart + _faCount) % _frameAlloc.Length;
        _frameAlloc[idx] = bytes;
        if (_faCount < _frameAlloc.Length) _faCount++;
        else _faStart = (_faStart + 1) % _frameAlloc.Length;
    }
    #endregion

    #region Fields
    private readonly Snapshot[] _samples;
    private int _start;
    private int _count;

    private readonly long[] _frameAlloc;
    private int _faStart;
    private int _faCount;

    private readonly int _windowMs;
    private readonly bool _cpuSupported;
    private long _exceptions;
    private bool _started;
    #endregion

    #region Types
    private readonly struct Snapshot(long timestamp, double cpuMs, long allocated, double gcPauseMs, long exceptions)
    {
        public readonly long Timestamp = timestamp;     // Stopwatch.GetTimestamp ticks (high resolution)
        public readonly double CpuMs = cpuMs;
        public readonly long Allocated = allocated;
        public readonly double GcPauseMs = gcPauseMs;
        public readonly long Exceptions = exceptions;
    }
    #endregion
}
