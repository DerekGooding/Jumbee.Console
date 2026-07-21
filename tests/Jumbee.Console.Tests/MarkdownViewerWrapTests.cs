namespace Jumbee.Console.Tests;

using System.Linq;

using Jumbee.Console;
using Jumbee.Console.Snapshot;

using Xunit;

/// <summary>Regression tests for <see cref="MarkdownViewer"/> paragraph word-wrap: a long paragraph must reflow to
/// the control width (previously it clipped at the right edge and dropped everything past the first line).</summary>
public class MarkdownViewerWrapTests
{
    private const string Para =
        "The passage of the sun across the sky drives the clock of life on Earth for many species.";

    [Fact]
    public void LongParagraph_WrapsToWidth_PreservingAllContent()
    {
        var text = ConsoleSnapshot.ToText(new MarkdownViewer(Para), 40, 12);

        Assert.Contains("species", text);   // the tail (past the first row) is no longer lost — the bug
        Assert.True(text.Split('\n').Count(l => l.Trim().Length > 0) >= 2, "the paragraph should span multiple rows");
    }

    [Fact]
    public void LongParagraph_WrapsAtWordBoundaries_NotMidWord()
    {
        var text = ConsoleSnapshot.ToText(new MarkdownViewer(Para), 40, 12);

        // With word-wrap each word stays intact, so it appears as a contiguous substring. Char-level wrapping would
        // split e.g. "drives" across a row break ("dr\nives"), and these Contains checks would fail.
        Assert.Contains("drives", text);
        Assert.Contains("Earth", text);
        Assert.Contains("clock", text);
    }
}
