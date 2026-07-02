namespace Jumbee.Console.Benchmarks;

using BenchmarkDotNet.Attributes;

using Spectre.Console;
using Spectre.Console.Rendering;

using Size = ConsoleGUI.Space.Size;

/// <summary>
/// Isolates the raw Spectre.Console segment/string work: producing segments from a renderable and the line/width
/// operations run over them. This is the layer where the class-vs-struct <c>Segment</c> and string-vs-slice
/// questions actually bite — measure here before touching <c>Segment</c>.
/// </summary>
[MemoryDiagnoser]
public class SegmentBenchmarks
{
    private AnsiConsoleBuffer _console = null!;
    private Table _table = null!;
    private Tree _tree = null!;
    private List<Segment> _tableSegments = null!;

    [Params(120)]
    public int Width;

    [GlobalSetup]
    public void Setup()
    {
        var buffer = new ConsoleBuffer { Size = new Size(Width, 60) };
        buffer.Initialize();
        _console = new AnsiConsoleBuffer(buffer);
        _table = Workloads.BuildTable();
        _tree = Workloads.BuildTree();
        _tableSegments = [.. _table.GetSegments(_console)];
    }

    [Benchmark(Baseline = true)]
    public int GetTableSegments()
    {
        var count = 0;
        foreach (var _ in _table.GetSegments(_console))
        {
            count++;
        }

        return count;
    }

    [Benchmark]
    public int GetTreeSegments()
    {
        var count = 0;
        foreach (var _ in _tree.GetSegments(_console))
        {
            count++;
        }

        return count;
    }

    [Benchmark]
    public List<SegmentLine> SplitLines() => Segment.SplitLines(_tableSegments, Width);

    [Benchmark]
    public List<Segment> Truncate() => Segment.Truncate(_tableSegments, Width - 40);
}
