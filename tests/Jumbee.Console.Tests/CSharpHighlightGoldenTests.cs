namespace Jumbee.Console.Tests;

using System.Security.Cryptography;
using System.Text;

using ColorCode;

using RazorConsole.Core.Rendering.Syntax;

using Spectre.Console;

using Xunit;

/// <summary>
/// Absolute golden test: pins the actual C# highlighted output (glyph + fg/bg/decoration per cell) so changes to
/// the ColorCode tokenizer (vendoring, regex options, scope/alloc refactors) can't silently alter highlighting.
/// The equivalence test can't cover this — it runs both formatters through the same ColorCode.
/// </summary>
public class CSharpHighlightGoldenTests
{
    private const string Source = """
        namespace Acme;

        using System;

        // A tiny widget.
        public sealed class Widget
        {
            private readonly string _name = "hi";
            public int Count { get; set; } = 0;
            public string Describe() => $"{_name}: {Count}";
        }
        """;

    [Fact]
    public void CSharpHighlight_MatchesGolden()
    {
        var options = new SyntaxOptions { TabWidth = 0 };
        var buffer = new ConsoleBuffer { Size = new ConsoleGUI.Space.Size(100, 40) };
        buffer.Initialize();
        var console = new AnsiConsoleBuffer(buffer) { wrap = true };
        console.Profile.Width = 200;
        console.Write(new SpectreSegmentFormatter().Format(Source, Languages.CSharp, SyntaxTheme.CreateDefault(), options));

        var sb = new StringBuilder();
        for (var y = 0; y < buffer.Size.Height; y++)
        {
            for (var x = 0; x < buffer.Size.Width; x++)
            {
                var ch = buffer[x, y].Character;
                if (ch.Content is null or ' ')
                {
                    continue;
                }

                sb.Append(x).Append(',').Append(y).Append(':').Append(ch.Content)
                  .Append('|').Append(ch.Foreground).Append('/').Append(ch.Background)
                  .Append('/').Append(ch.Decoration).Append(';');
            }
        }

        var hash = System.Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(sb.ToString())));

        // Captured from ColorCode.Core 2.0.15 (NuGet) before vendoring/optimization.
        Assert.Equal("B3A146A9A7EFCB62706F1B70E24CBA1AAE5642EDFF9AE1047181C160AB875D11", hash);
    }
}
