namespace Jumbee.Console.Benchmarks;

using BenchmarkDotNet.Attributes;

using ColorCode;

using RazorConsole.Core.Rendering.Syntax;

using Spectre.Console;

using Size = ConsoleGUI.Space.Size;

/// <summary>
/// The C# syntax-highlight burst the editor pays on every document switch (MultiTabCodeEditor source pane).
/// The current path is: ColorCode tokenizes → <see cref="SpectreMarkupFormatter"/> builds a Spectre **markup
/// string** → <c>ansiConsole.Markup(str)</c> **re-parses** that string into segments → the buffer applies them.
/// The markup string is a pure intermediate that is parsed twice. <see cref="FormatCSharp"/> isolates the
/// tokenize+markup-build cost; <see cref="HighlightCSharp"/> measures the full editor burst (the "before" number).
/// </summary>
[MemoryDiagnoser]
public class SyntaxHighlightBenchmarks
{
    private const int Repeat = 30; // ~450 lines — a realistic mid-size source file.

    private static readonly string Source = BuildSource();

    private ConsoleBuffer _buffer = null!;
    private AnsiConsoleBuffer _console = null!;
    private SpectreMarkupFormatter _formatter = null!;
    private SpectreSegmentFormatter _segmentFormatter = null!;
    private SyntaxTheme _theme = null!;
    private SyntaxOptions _options = null!;

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
        _formatter = new SpectreMarkupFormatter();
        _segmentFormatter = new SpectreSegmentFormatter();
        _theme = SyntaxTheme.CreateDefault();
        _options = new SyntaxOptions { TabWidth = 0 };
        // Mirror the editor: render each logical line unwrapped (inflated profile width) so Spectre never
        // word-wraps; the buffer applies its own character wrap. See TextEditor.WriteText.
        _console.Profile.Width = LongestLineWidth(Source) + 1;
    }

    // Tokenize + build the markup string only (no re-parse, no buffer write).
    [Benchmark(Baseline = true)]
    public string FormatCSharp() => _formatter.Format(Source, Languages.CSharp, _theme, _options);

    // The full editor burst: format → re-parse the markup string → apply segments to the buffer.
    [Benchmark]
    public void HighlightCSharp()
    {
        _buffer.Initialize();
        _console.Markup(_formatter.Format(Source, Languages.CSharp, _theme, _options));
    }

    // Phase 1: emit Spectre segments directly — no markup string, no escape, no re-parse.
    [Benchmark]
    public IReadOnlyList<Spectre.Console.Rendering.Segment> FormatCSharpSegments() =>
        _segmentFormatter.Format(Source, Languages.CSharp, _theme, _options);

    // Phase 1 full burst: format to segments → apply straight to the buffer (no markup round-trip).
    [Benchmark]
    public void HighlightCSharpSegments()
    {
        _buffer.Initialize();
        _console.Write(_segmentFormatter.Format(Source, Languages.CSharp, _theme, _options));
    }

    private static int LongestLineWidth(string text)
    {
        int max = 1, col = 0;
        foreach (var c in text)
        {
            if (c == '\n') { if (col > max) max = col; col = 0; }
            else if (c != '\r') col++;
        }
        return Math.Max(max, col);
    }

    private static string BuildSource()
    {
        const string block = """
            namespace Acme.Widgets;

            using System;
            using System.Collections.Generic;

            /// <summary>Represents a widget in the catalog.</summary>
            public sealed class Widget : IEquatable<Widget>
            {
                private readonly string _name;
                private int _count = 0;

                public Widget(string name, int count = 0)
                {
                    _name = name ?? throw new ArgumentNullException(nameof(name));
                    _count = count;
                }

                public string Name => _name;

                public int Increment() => ++_count;

                // Formats a short description for display.
                public string Describe() => $"{_name}: {_count} item(s)";

                public bool Equals(Widget? other) =>
                    other is not null && string.Equals(_name, other._name, StringComparison.Ordinal);

                public override int GetHashCode() => _name.GetHashCode();
            }
            """;
        return string.Join("\n\n", Enumerable.Repeat(block, Repeat));
    }
}
