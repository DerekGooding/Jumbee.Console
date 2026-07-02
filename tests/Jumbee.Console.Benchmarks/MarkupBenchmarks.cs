namespace Jumbee.Console.Benchmarks;

using BenchmarkDotNet.Attributes;

using Spectre.Console;

/// <summary>
/// The markup parse path: tokenizer (StringBuffer + per-token StringBuilder), StyleParser on open tags,
/// Emoji.Replace, and Paragraph.Append/SplitWords on text tokens. `new Markup(text)` parses eagerly.
/// </summary>
[MemoryDiagnoser]
public class MarkupBenchmarks
{
    // Styled spans + nesting + a link + a long run of plain prose (the common bulk of a real line).
    private const string Rich =
        "[bold]Name:[/] [green]item-42[/]  [yellow]WARN[/]  " +
        "[link=https://example.com/path]details[/]  " +
        "plain trailing text that has no markup at all and is the common bulk of a rendered line";

    // Pure text — exercises only the tokenizer text fast path + word splitting.
    private const string Plain =
        "just some plain text with no markup tags at all, the fast path for text tokens in the tokenizer";

    // Deeply nested styles — exercises the open/close style stack.
    private const string Nested =
        "[red]a [bold]b [underline]c[/] d[/] e[/] normal [blue]f[/] tail";

    // Escaped brackets — forces the tokenizer's StringBuilder collapse path ([[ -> [, ]] -> ]).
    private const string Escaped =
        "an array like [[1, 2, 3]] and a [[key]] plus [green]colored[/] text mixed in for good measure";

    // A long span of markup: a syntax-highlighted C# document (as a highlighter would emit), ~60 lines. Exercises
    // the parser at document scale — many open/close tags, text tokens, and newlines.
    private static readonly string CodeDocument = BuildCodeDocument();

    private static string BuildCodeDocument()
    {
        var block = string.Join('\n',
            "[blue]namespace[/] [green]Acme.Widgets[/];",
            string.Empty,
            "[grey]// Represents a widget in the catalog.[/]",
            "[blue]public sealed class[/] [green]Widget[/]",
            "{",
            "    [blue]private readonly[/] [green]string[/] _name;",
            "    [blue]private[/] [green]int[/] _count [grey]=[/] [magenta]0[/];",
            string.Empty,
            "    [blue]public[/] [green]Widget[/]([green]string[/] name)",
            "    {",
            "        _name [grey]=[/] name [grey]??[/] [blue]throw new[/] [green]ArgumentNullException[/]([yellow]\"name\"[/]);",
            "    }",
            string.Empty,
            "    [blue]public[/] [green]int[/] [cyan]Increment[/]() [grey]=>[/] [grey]++[/]_count;",
            "    [blue]public[/] [green]string[/] [cyan]Describe[/]() [grey]=>[/] [yellow]$\"{_name}: {_count}\"[/];",
            "}");
        return string.Join('\n', Enumerable.Repeat(block, 4));
    }

    [Benchmark(Baseline = true)]
    public Markup ParseRich() => new Markup(Rich);

    [Benchmark]
    public Markup ParsePlain() => new Markup(Plain);

    [Benchmark]
    public Markup ParseNested() => new Markup(Nested);

    [Benchmark]
    public Markup ParseEscaped() => new Markup(Escaped);

    [Benchmark]
    public Markup ParseCodeDocument() => new Markup(CodeDocument);
}
