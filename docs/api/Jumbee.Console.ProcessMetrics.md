# <a id="Jumbee_Console_ProcessMetrics"></a> Class ProcessMetrics

Namespace: [Jumbee.Console](Jumbee.Console.md)  
Assembly: Jumbee.Console.dll  

Collects live process/runtime performance metrics for the perf HUD by reading the runtime APIs the
<code>System.Runtime</code> meter wraps (<xref href="System.GC" data-throw-if-not-resolved="false"></xref>, <xref href="System.Environment" data-throw-if-not-resolved="false"></xref>, <xref href="System.Threading.ThreadPool" data-throw-if-not-resolved="false"></xref>,
<xref href="System.Threading.Monitor" data-throw-if-not-resolved="false"></xref>) directly — no <code>MeterListener</code>, so there is nothing to sample-schedule and no
observable-instrument staleness.

```csharp
public sealed class ProcessMetrics
```

#### Inheritance

object ← 
[ProcessMetrics](Jumbee.Console.ProcessMetrics.md)

## Remarks

<p>Two measurement styles: <xref href="Jumbee.Console.ProcessMetrics.RecordFrame(System.Double%2cSystem.Double%2cSystem.Int64%2cSystem.Boolean%2cSystem.Double)" data-throw-if-not-resolved="false"></xref> takes the <em>directly measured</em> cost of one
draw/paint cycle (high-resolution wall time + allocation delta, bracketed around the render in the UI loop) —
this is immune to the coarse (~15 ms) resolution of process CPU time and, read as a <em>peak</em>, surfaces
bursts an average would hide. <xref href="Jumbee.Console.ProcessMetrics.Sample" data-throw-if-not-resolved="false"></xref> snapshots cumulative process counters (CPU time, GC pause,
exceptions), differenced over a rolling window for whole-process rates.</p>
<p>All state is written and read on the UI thread; only the first-chance exception count is cross-thread
(it uses <xref href="System.Threading.Interlocked" data-throw-if-not-resolved="false"></xref>).</p>

## Constructors

### <a id="Jumbee_Console_ProcessMetrics__ctor_System_Int32_System_Int32_"></a> ProcessMetrics\(int, int\)

```csharp
public ProcessMetrics(int capacity = 128, int windowMs = 1000)
```

#### Parameters

`capacity` int

Number of snapshots/frames retained (covers <code class="paramref">windowMs</code> at the frame
    rate; the default covers several seconds at a 100 ms tick).

`windowMs` int

Rolling window over which whole-process rates (CPU%, GC-pause%, exceptions/s) are measured.

## Properties

### <a id="Jumbee_Console_ProcessMetrics_AllocatedBytesPerFrame"></a> AllocatedBytesPerFrame

Mean bytes allocated on the managed heap per draw/paint cycle.

```csharp
public double AllocatedBytesPerFrame { get; }
```

#### Property Value

 double

#### Remarks

The headline retained-mode number: an idle UI allocates ~nothing per frame.

### <a id="Jumbee_Console_ProcessMetrics_AllocatedBytesPerSecond"></a> AllocatedBytesPerSecond

Managed heap allocation rate over the rolling window, in bytes per second (whole process).

```csharp
public double AllocatedBytesPerSecond { get; }
```

#### Property Value

 double

### <a id="Jumbee_Console_ProcessMetrics_BusyPercentAvg"></a> BusyPercentAvg

Mean UI-thread utilisation over the retained frames (0..100) — the typical busy fraction, which
    stays low for a retained UI while <xref href="Jumbee.Console.ProcessMetrics.BusyPercentPeak" data-throw-if-not-resolved="false"></xref> spikes on a burst frame.

```csharp
public double BusyPercentAvg { get; }
```

#### Property Value

 double

### <a id="Jumbee_Console_ProcessMetrics_BusyPercentPeak"></a> BusyPercentPeak

Peak UI-thread utilisation over the retained frames (0..100): the busiest frame's render time as a
    fraction of that frame's period.

```csharp
public double BusyPercentPeak { get; }
```

#### Property Value

 double

#### Remarks

~0 when idle (short render, long wait), rising toward 100 when frames run back-to-back under load.
    High-resolution, so unlike process CPU% it reflects brief bursts.

### <a id="Jumbee_Console_ProcessMetrics_CpuSupported"></a> CpuSupported

<a href="https://learn.microsoft.com/dotnet/csharp/language-reference/builtin-types/bool">true</a> if per-process CPU time is available on this OS (see the constructor guard).

```csharp
public bool CpuSupported { get; }
```

#### Property Value

 bool

### <a id="Jumbee_Console_ProcessMetrics_CpuUsagePercent"></a> CpuUsagePercent

Whole-process CPU usage over the rolling window, as a percentage of total machine capacity (user +
    kernel, divided by <xref href="System.Environment.ProcessorCount" data-throw-if-not-resolved="false"></xref>) — the same figure Task Manager shows for the
    process. 0 when unavailable.

```csharp
public double CpuUsagePercent { get; }
```

#### Property Value

 double

#### Remarks

<em>Coarse</em>: process CPU time advances in ~15 ms OS ticks, so it reflects sustained load but
    not brief per-frame bursts (use <xref href="Jumbee.Console.ProcessMetrics.BusyPercentPeak" data-throw-if-not-resolved="false"></xref> / <xref href="Jumbee.Console.ProcessMetrics.RenderTimeMsPeak" data-throw-if-not-resolved="false"></xref> for those).

### <a id="Jumbee_Console_ProcessMetrics_DirtyAreaPercentAvg"></a> DirtyAreaPercentAvg

Mean fraction of the screen (0..100) re-composited on the frames that <em>did</em> redraw.

```csharp
public double DirtyAreaPercentAvg { get; }
```

#### Property Value

 double

#### Remarks

With dirty-rectangle rendering a small change (a status-bar tick) touches only its own rows, so this
    reads a few percent even while <xref href="Jumbee.Console.ProcessMetrics.RedrawPercent" data-throw-if-not-resolved="false"></xref> is high; only a resize/theme-switch pushes a frame
    to 100.

### <a id="Jumbee_Console_ProcessMetrics_DirtyAreaPercentPeak"></a> DirtyAreaPercentPeak

Largest fraction of the screen (0..100) re-composited in a single frame over the window — a resize
    pushes this to 100; steady interaction keeps it small.

```csharp
public double DirtyAreaPercentPeak { get; }
```

#### Property Value

 double

### <a id="Jumbee_Console_ProcessMetrics_ExceptionsPerSecond"></a> ExceptionsPerSecond

First-chance managed exceptions per second over the rolling window (thrown, caught or not).

```csharp
public double ExceptionsPerSecond { get; }
```

#### Property Value

 double

### <a id="Jumbee_Console_ProcessMetrics_GcPausePercent"></a> GcPausePercent

Fraction of wall time (0..100) the runtime spent paused for GC over the rolling window — the
    clearest signal that GC is hitching the UI.

```csharp
public double GcPausePercent { get; }
```

#### Property Value

 double

### <a id="Jumbee_Console_ProcessMetrics_Gen0Collections"></a> Gen0Collections

Cumulative number of generation-0 garbage collections since process start.

```csharp
public int Gen0Collections { get; }
```

#### Property Value

 int

### <a id="Jumbee_Console_ProcessMetrics_Gen1Collections"></a> Gen1Collections

Cumulative number of generation-1 garbage collections since process start.

```csharp
public int Gen1Collections { get; }
```

#### Property Value

 int

### <a id="Jumbee_Console_ProcessMetrics_Gen2Collections"></a> Gen2Collections

Cumulative number of generation-2 garbage collections since process start.

```csharp
public int Gen2Collections { get; }
```

#### Property Value

 int

### <a id="Jumbee_Console_ProcessMetrics_LockContentions"></a> LockContentions

Total monitor lock contentions since process start — the no-lock dagger; 0 for the single-threaded UI.

```csharp
public long LockContentions { get; }
```

#### Property Value

 long

### <a id="Jumbee_Console_ProcessMetrics_ManagedHeapBytes"></a> ManagedHeapBytes

Current managed heap size (<xref href="System.GC.GetTotalMemory(System.Boolean)" data-throw-if-not-resolved="false"></xref>), in bytes.

```csharp
public long ManagedHeapBytes { get; }
```

#### Property Value

 long

### <a id="Jumbee_Console_ProcessMetrics_PeakAllocatedBytesPerFrame"></a> PeakAllocatedBytesPerFrame

Peak bytes allocated in a single draw/paint cycle over the retained frames — surfaces an allocation
    burst an average would dilute away.

```csharp
public long PeakAllocatedBytesPerFrame { get; }
```

#### Property Value

 long

### <a id="Jumbee_Console_ProcessMetrics_RedrawPercent"></a> RedrawPercent

Percentage of retained frames that actually redrew the screen (took the full draw path) versus those
    that idled (cheap resize-check only).

```csharp
public double RedrawPercent { get; }
```

#### Property Value

 double

#### Remarks

The retained-mode headline: a mostly-static UI redraws only the few frames where content changed, so
    this sits low and climbs toward 100 only under interaction/animation.

### <a id="Jumbee_Console_ProcessMetrics_RenderTimeMsAvg"></a> RenderTimeMsAvg

Mean draw/paint cycle wall time over the retained frames, in milliseconds.

```csharp
public double RenderTimeMsAvg { get; }
```

#### Property Value

 double

### <a id="Jumbee_Console_ProcessMetrics_RenderTimeMsPeak"></a> RenderTimeMsPeak

Longest draw/paint cycle over the retained frames, in milliseconds — the peak render cost, which a
    burst (e.g. a paste re-rendering the editor) pushes up and which lingers until it ages out of the window.

```csharp
public double RenderTimeMsPeak { get; }
```

#### Property Value

 double

### <a id="Jumbee_Console_ProcessMetrics_ThreadPoolQueueLength"></a> ThreadPoolQueueLength

Number of work items currently queued to the thread pool.

```csharp
public long ThreadPoolQueueLength { get; }
```

#### Property Value

 long

### <a id="Jumbee_Console_ProcessMetrics_ThreadPoolThreadCount"></a> ThreadPoolThreadCount

Current number of thread-pool worker threads.

```csharp
public int ThreadPoolThreadCount { get; }
```

#### Property Value

 int

### <a id="Jumbee_Console_ProcessMetrics_WorkingSetBytes"></a> WorkingSetBytes

Physical memory mapped to the process (<xref href="System.Environment.WorkingSet" data-throw-if-not-resolved="false"></xref>) right now, in bytes —
    an instantaneous gauge, not a rate.

```csharp
public long WorkingSetBytes { get; }
```

#### Property Value

 long

#### Remarks

Use <xref href="Jumbee.Console.ProcessMetrics.WorkingSetBytesAvg" data-throw-if-not-resolved="false"></xref>/<xref href="Jumbee.Console.ProcessMetrics.WorkingSetBytesPeak" data-throw-if-not-resolved="false"></xref> for the windowed figures.

### <a id="Jumbee_Console_ProcessMetrics_WorkingSetBytesAvg"></a> WorkingSetBytesAvg

Mean working set over the retained snapshots (sampled once per frame), in bytes.

```csharp
public double WorkingSetBytesAvg { get; }
```

#### Property Value

 double

#### Remarks

Since the working set is sticky, this tracks close to the current value — a resize that grows it
    lifts the average and it stays up (unlike the alloc average, which falls back once the burst frame ages
    out).

### <a id="Jumbee_Console_ProcessMetrics_WorkingSetBytesPeak"></a> WorkingSetBytesPeak

Highest working set over the retained snapshots, in bytes — the footprint high-water mark within the
    window (a resize/paste burst pushes it up and it lingers until it ages out).

```csharp
public long WorkingSetBytesPeak { get; }
```

#### Property Value

 long

## Methods

### <a id="Jumbee_Console_ProcessMetrics_Dispose"></a> Dispose\(\)

Stops collection and releases resources.

```csharp
public void Dispose()
```

### <a id="Jumbee_Console_ProcessMetrics_RecordFrame_System_Double_System_Double_System_Int64_System_Boolean_System_Double_"></a> RecordFrame\(double, double, long, bool, double\)

Records the directly-measured cost of one draw/paint cycle (from the UI loop, which brackets the
    render), and takes a cumulative sample for the windowed process rates.

```csharp
public void RecordFrame(double renderMs, double periodMs, long renderAllocBytes, bool redrawn = false, double dirtyAreaFraction = 0)
```

#### Parameters

`renderMs` double

Wall time the draw/paint cycle took (high-resolution).

`periodMs` double

Wall time since the previous frame started (for utilisation).

`renderAllocBytes` long

Bytes allocated on the managed heap during the cycle.

`redrawn` bool

<a href="https://learn.microsoft.com/dotnet/csharp/language-reference/builtin-types/bool">true</a> if this frame took the full draw path; <a href="https://learn.microsoft.com/dotnet/csharp/language-reference/builtin-types/bool">false</a>
    for an idle frame. Feeds <xref href="Jumbee.Console.ProcessMetrics.RedrawPercent" data-throw-if-not-resolved="false"></xref>.

`dirtyAreaFraction` double

Fraction (0..1) of the screen re-composited this frame. Feeds
    <xref href="Jumbee.Console.ProcessMetrics.DirtyAreaPercentAvg" data-throw-if-not-resolved="false"></xref>/<xref href="Jumbee.Console.ProcessMetrics.DirtyAreaPercentPeak" data-throw-if-not-resolved="false"></xref> (only counted on redrawn frames).

#### Remarks

Call once per frame on the UI thread.

### <a id="Jumbee_Console_ProcessMetrics_Sample"></a> Sample\(\)

Snapshots the cumulative process counters (for the windowed rates).

```csharp
public void Sample()
```

#### Remarks

Called by <xref href="Jumbee.Console.ProcessMetrics.RecordFrame(System.Double%2cSystem.Double%2cSystem.Int64%2cSystem.Boolean%2cSystem.Double)" data-throw-if-not-resolved="false"></xref> once per frame; exposed for callers/tests without a frame loop.

### <a id="Jumbee_Console_ProcessMetrics_Start"></a> Start\(\)

Begins collection: clears the retained frame/snapshot history (so a new UI session doesn't average
    in the previous one's frames), subscribes to first-chance exceptions, and takes a baseline cumulative sample.

```csharp
public void Start()
```

### <a id="Jumbee_Console_ProcessMetrics_Stop"></a> Stop\(\)

Stops collection and unsubscribes.

```csharp
public void Stop()
```

