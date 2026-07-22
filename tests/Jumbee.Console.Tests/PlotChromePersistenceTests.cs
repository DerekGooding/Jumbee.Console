namespace Jumbee.Console.Tests;

using System;
using System.Linq;

using Jumbee.Console.Snapshot;

using Xunit;

/// <summary>Regression: axis/grid/tick styling set via <c>Configure*</c> must survive <see cref="Plot.Clear"/>, so a
/// plot rebuilt each frame with <c>Clear()</c> + <c>AddSeries</c> (the natural live-data pattern) keeps its configured
/// chrome. Surfaced by the scope-tui port: a once-configured hidden grid reappeared on the first frame after setup
/// because <c>Clear()</c> dropped the configuration along with the data.</summary>
public class PlotChromePersistenceTests
{
    private static string RenderSine(Plot p)
    {
        var xs = Enumerable.Range(0, 64).Select(i => (double)i).ToArray();
        var ys = xs.Select(x => Math.Sin(x * 0.2)).ToArray();
        p.AddSeries(xs, ys, PlotBrush.Braille);
        return ConsoleSnapshot.ToText(p, 50, 16);
    }

    // Numeric tick labels are the only source of digits in a bare sine plot, so a digit is a reliable proxy for
    // "tick-label chrome rendered".
    private static bool HasDigit(string s) => s.Any(char.IsDigit);

    [Fact]
    public void ConfigureChrome_SurvivesClearAndPerFrameRebuild()
    {
        // Default plot renders numeric tick labels (digits present) — the chrome we intend to hide.
        Assert.True(HasDigit(RenderSine(new Plot())), "a default plot should render numeric tick labels");

        var bare = new Plot();
        bare.ConfigureGrid(g => g.IsVisible = false)
            .ConfigureTicks(t => { t.IsVisible = false; t.Labels.IsVisible = false; });
        Assert.False(HasDigit(RenderSine(bare)), "tick labels should be hidden once configured");

        // Simulate the live-data loop: Clear() then re-add series each frame. The hidden chrome must persist.
        for (var frame = 0; frame < 3; frame++)
        {
            bare.Clear();
            Assert.False(HasDigit(RenderSine(bare)), $"frame {frame}: hidden tick labels must not reappear after Clear()");
        }
    }
}
