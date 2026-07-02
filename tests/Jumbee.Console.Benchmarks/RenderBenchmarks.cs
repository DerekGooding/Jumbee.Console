namespace Jumbee.Console.Benchmarks;

using BenchmarkDotNet.Attributes;

using Spectre.Console;

using Size = ConsoleGUI.Space.Size;

/// <summary>
/// The end-to-end per-frame render cost as Jumbee actually pays it: a Spectre renderable rendered through
/// <see cref="AnsiConsoleBuffer"/> into a <see cref="ConsoleBuffer"/> (GetSegments + segment application to cells).
/// Each iteration re-clears the buffer, mirroring a redraw. Compare deltas here against <see cref="SegmentBenchmarks"/>
/// to see whether a segment-level win survives to the frame level.
/// </summary>
[MemoryDiagnoser]
public class RenderBenchmarks
{
    private ConsoleBuffer _buffer = null!;
    private AnsiConsoleBuffer _console = null!;
    private Table _table = null!;
    private Tree _tree = null!;

    [Params(120)]
    public int Width;

    [Params(40)]
    public int Height;

    [GlobalSetup]
    public void Setup()
    {
        _buffer = new ConsoleBuffer { Size = new Size(Width, Height) };
        _buffer.Initialize();
        _console = new AnsiConsoleBuffer(_buffer);
        _table = Workloads.BuildTable();
        _tree = Workloads.BuildTree();
    }

    [Benchmark(Baseline = true)]
    public void RenderTable()
    {
        _buffer.Initialize();
        _console.Write(_table);
    }

    [Benchmark]
    public void RenderTree()
    {
        _buffer.Initialize();
        _console.Write(_tree);
    }
}
