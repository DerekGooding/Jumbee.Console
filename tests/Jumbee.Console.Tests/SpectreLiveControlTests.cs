namespace Jumbee.Console.Tests;

using System;
using System.Threading;
using System.Threading.Tasks;

using Jumbee.Console;
using Jumbee.Console.Snapshot;

using Xunit;
using Xunit.Abstractions;

using S = Spectre.Console;

/// <summary>
/// The Spectre live widgets — <see cref="SpectreLiveDisplay"/> and <see cref="SpectreTaskProgress"/> — refreshing
/// from their own threads into a control's buffer, and, crucially, alongside a control that still takes input. Plain
/// Spectre.Console owns stdout and the input loop for the duration of a live widget's callback; the whole point of
/// wrapping them is that here they are just controls.
/// </summary>
public class SpectreLiveControlTests
{
    private readonly ITestOutputHelper _out;
    public SpectreLiveControlTests(ITestOutputHelper output)
    {
        _out = output;
        UiTestHarness.EnsureStopped();
    }

    // Pumps frames until `until` holds or we give up, so the test doesn't race the widget's own refresh thread.
    private static async Task<bool> WaitForAsync(AnsiConsoleSession session, Func<string, bool> until, int frames = 60)
    {
        for (var i = 0; i < frames; i++)
        {
            await session.FrameAsync();
            if (until(ConsoleSnapshot.ToText(session.Screen.Buffer))) return true;
            await Task.Delay(20);
        }
        return false;
    }

    /// <summary>
    /// The regression this exists for: Spectre's Progress picks its live renderer only when the console reports
    /// Interactive AND Ansi, otherwise it silently substitutes FallbackProgressRenderer, which draws nothing.
    /// AnsiConsoleBuffer used to copy both from the ambient AnsiConsole — i.e. from detection against the host
    /// process's stdout — so the control rendered nothing whenever output was redirected (a pipe, CI, a debugger).
    /// A buffer is not stdout: it always accepts styled segments and Jumbee re-composites it every frame, so it now
    /// reports both intrinsically. Simulated here by forcing the ambient profile non-interactive, since the test
    /// host's own detection varies.
    /// </summary>
    [Fact]
    public async Task TaskProgress_RendersBars_EvenWhenTheHostConsoleIsNotInteractive()
    {
        var caps = S.AnsiConsole.Profile.Capabilities;
        var (wasInteractive, wasAnsi) = (caps.Interactive, caps.Ansi);
        caps.Interactive = false;
        caps.Ansi = false;
        try
        {
            // Constructed while the ambient console looks dead: the buffer must not inherit that.
            var progress = new SpectreTaskProgress().AddColumns(
                new S.TaskDescriptionColumn(), new S.ProgressBarColumn(), new S.PercentageColumn());
            using var cancel = new CancellationTokenSource();
            using var session = await AnsiConsoleSession.StartAsync(progress, 60, 8);

            progress.Start(ctx =>
            {
                var task = ctx.AddTask("Indexing");
                while (!cancel.IsCancellationRequested)
                {
                    task.Increment(2);
                    if (task.IsFinished) task.Value = 0;
                    if (cancel.Token.WaitHandle.WaitOne(20)) break;
                }
            });

            // Assert on the BAR, not on the description or the percentage: FallbackProgressRenderer prints those
            // too (as milestone lines at 25/50/75%…), so either of those would pass in the broken state. Only the
            // live DefaultProgressRenderer draws a bar. Accept either glyph — ProgressBar uses '━' or, when the
            // console reports no Unicode (which the test host does, and which the buffer inherits by design since
            // it describes the real output device), '-'.
            var shown = await WaitForAsync(session, text =>
                text.Contains("Indexing") && (text.Contains(new string('━', 10)) || text.Contains(new string('-', 10))));
            cancel.Cancel();

            _out.WriteLine(ConsoleSnapshot.ToText(session.Screen.Buffer));
            Assert.True(shown, "the live progress renderer should have drawn a bar; only the fallback renderer, " +
                               "which draws none, is used when the console reports itself non-interactive");
        }
        finally
        {
            caps.Interactive = wasInteractive;
            caps.Ansi = wasAnsi;
        }
    }

    [Fact]
    public async Task LiveDisplay_ShowsUpdatedTargets()
    {
        var live = new SpectreLiveDisplay(new S.Markup("starting"));
        using var cancel = new CancellationTokenSource();
        using var session = await AnsiConsoleSession.StartAsync(live, 60, 8);

        var tick = 0;
        live.Start(ctx =>
        {
            while (!cancel.IsCancellationRequested)
            {
                ctx.UpdateTarget(new S.Markup($"tick {++tick}"));
                ctx.Refresh();
                if (cancel.Token.WaitHandle.WaitOne(20)) break;
            }
        });

        var shown = await WaitForAsync(session, text => text.Contains("tick "));
        cancel.Cancel();

        Assert.True(shown, "the live display should have rendered an updated target");
    }

    /// <summary>The headline claim of both wrappers: a live widget refreshing from its own thread does not block
    /// input to anything else. Under plain Spectre.Console this scenario cannot be expressed at all.</summary>
    [Fact]
    public async Task InputStillWorks_WhileALiveWidgetRefreshes()
    {
        var live = new SpectreLiveDisplay(new S.Markup("starting"));
        var input = new TextInput();
        var layout = new Grid([4, 1], [40], [[live], [input]]);

        using var cancel = new CancellationTokenSource();
        using var session = await AnsiConsoleSession.StartAsync(layout, 40, 5);

        live.Start(ctx =>
        {
            var tick = 0;
            while (!cancel.IsCancellationRequested)
            {
                ctx.UpdateTarget(new S.Markup($"tick {++tick}"));
                ctx.Refresh();
                if (cancel.Token.WaitHandle.WaitOne(15)) break;
            }
        });

        Assert.True(await WaitForAsync(session, t => t.Contains("tick ")), "the live display should be refreshing");

        // Type into the field while the live widget keeps refreshing.
        input.Focus();
        foreach (var c in "hello")
        {
            layout.OnInput(new UI.InputEventArgs(new ConsoleGUI.Input.InputEvent(
                new ConsoleKeyInfo(c, ConsoleKey.None, false, false, false))));
            await session.FrameAsync();
        }

        var typed = await WaitForAsync(session, t => t.Contains("hello"));
        cancel.Cancel();

        _out.WriteLine(ConsoleSnapshot.ToText(session.Screen.Buffer));
        Assert.Equal("hello", input.Text);
        Assert.True(typed, "the typed text should be on screen alongside the live widget");
    }
}
