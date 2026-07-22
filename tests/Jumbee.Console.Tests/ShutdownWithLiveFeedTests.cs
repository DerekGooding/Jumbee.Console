namespace Jumbee.Console.Tests;

using System.Threading;
using System.Threading.Tasks;

using ConsoleGUI.Api;
using ConsoleGUI.Data;
using ConsoleGUI.Space;

using Jumbee.Console;

using Xunit;

// The examples app's quit path is "stop the live feed, then UI.Stop". This verifies the shutdown invariants that
// makes the app actually exit: (1) UI.Stop completes the run task even while a background feed floods UI.Post at a
// high rate (no producer starves the drain so the loop never sees the stop); (2) cancelling the feed's token
// actually stops it. All UI threads are background, so once run completes the process exits.
public class ShutdownWithLiveFeedTests
{
    public ShutdownWithLiveFeedTests() => UiTestHarness.EnsureStopped();

    [Fact]
    public void UiStop_CompletesRun_EvenWithABackgroundFeedFloodingPosts()
    {
        UiTestHarness.EnsureStopped();
        var origOut = System.Console.Out;
        System.Console.SetOut(System.IO.TextWriter.Null);

        var feedCts = new CancellationTokenSource();
        var label = new TextLabel(TextLabelOrientation.Horizontal, "x");
        Task run;
        try
        {
            run = UI.Start(new VerticalStackPanel(label), 40, 8, fps: 66, isAnsiTerminal: true,
                console: new StubConsole(40, 8), input: new NoInput());

            // A live feed like the dashboards use: post work onto the UI thread continuously from a background task.
            var ct = feedCts.Token;
            int posts = 0;
            _ = Task.Run(async () =>
            {
                while (!ct.IsCancellationRequested)
                {
                    UI.Post(() => Interlocked.Increment(ref posts));
                    try { await Task.Delay(1, ct); } catch { break; }
                }
            });

            Thread.Sleep(200);   // let the feed run and the loop spin
            Assert.True(Volatile.Read(ref posts) > 0, "the feed should have posted work");

            // Quit like the app: stop the feed, then the UI loop (from this non-UI thread).
            feedCts.Cancel();
            UI.Stop();

            // run must complete promptly — a flooding producer must not keep the loop from seeing the stop.
            Assert.True(run.Wait(3000), "UI.Stop must complete the run task even under a post flood");
            Assert.True(run.IsCompletedSuccessfully);
        }
        finally
        {
            feedCts.Cancel();
            UI.Stop();
            System.Console.SetOut(origOut);
        }
    }

    private sealed class NoInput : IInputSource
    {
        public bool TryRead(out TerminalInputEvent? evt) { evt = null; return false; }
    }

    private sealed class StubConsole(int w, int h) : IConsole
    {
        public Size Size { get; set; } = new Size(w, h);
        public bool KeyAvailable => false;
        public void Initialize() { }
        public void OnRefresh() { }
        public void Write(Position position, in Character character) { }
        public ConsoleKeyInfo ReadKey() => throw new System.NotSupportedException();
    }
}
