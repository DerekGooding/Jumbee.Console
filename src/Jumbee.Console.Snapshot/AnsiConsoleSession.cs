namespace Jumbee.Console.Snapshot;

using System;
using System.Text;
using System.Threading.Tasks;

using ConsoleGUI;
using ConsoleGUI.Space;

using Vezel.Cathode.Text.Control;

/// <summary>
/// A stateful counterpart to <see cref="AnsiConsoleSnapshot"/> for testing the <em>live</em> render: it keeps
/// <see cref="ConsoleManager"/> set up across multiple frames so each <see cref="FrameAsync"/> emits an
/// incremental diff against the persistent buffer (not a fresh full frame), and folds those bytes into a
/// persistent <see cref="Screen"/> — exactly as a real terminal accumulates them. Use it to reproduce diff/cursor
/// or async-ordering bugs that only appear across frames (e.g. press → release). Dispose to restore the sink.
/// </summary>
public sealed class AnsiConsoleSession : IDisposable
{
    #region Constructors
    private AnsiConsoleSession(int width, int height)
    {
        _prevOutput = ConsoleManager.AnsiOutput;
        _prevAnsi = ConsoleManager.AnsiEnabled;
        Screen = new AnsiScreen(width, height);

        ConsoleManager.AnsiEnabled = true;
        // Mirror production: write off the UI thread, serialized by ConsoleManager (OutputIdle makes it awaitable).
        ConsoleManager.AnsiOutput = acsb =>
        {
            var s = acsb.ToString();
            return Task.Run(() => { lock (_capture) _capture.Append(s); });
        };
    }
    #endregion

    #region Properties
    /// <summary>The accumulated screen after every frame folded in so far.</summary>
    public AnsiScreen Screen { get; }
    #endregion

    #region Methods
    /// <summary>Sets up the console for <paramref name="content"/> and folds in the initial frame.</summary>
    public static async Task<AnsiConsoleSession> StartAsync(IControl content, int width, int height)
    {
        var session = new AnsiConsoleSession(width, height);
        ConsoleManager.Console = new HeadlessConsole { Size = new Size(width, height) };
        ConsoleManager.Setup();
        ConsoleManager.Content = content;
        await session.FrameAsync().ConfigureAwait(false);
        return session;
    }

    /// <summary>Paints pending control state, draws one frame (an incremental diff), waits for the serialized
    /// output, and folds the emitted bytes into <see cref="Screen"/>.</summary>
    public async Task FrameAsync()
    {
        UI.PaintFrame();
        ConsoleManager.Draw();
        await ConsoleManager.OutputIdle.ConfigureAwait(false);
        lock (_capture)
        {
            Screen.Feed(_capture.ToString());
            _capture.Clear();
        }
    }

    public void Dispose()
    {
        ConsoleManager.AnsiOutput = _prevOutput;
        ConsoleManager.AnsiEnabled = _prevAnsi;
    }
    #endregion

    #region Fields
    private readonly StringBuilder _capture = new();
    private readonly Func<AnsiControlSequenceBuilder, Task> _prevOutput;
    private readonly bool _prevAnsi;
    #endregion
}
