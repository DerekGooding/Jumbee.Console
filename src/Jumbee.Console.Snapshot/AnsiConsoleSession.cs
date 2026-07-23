
using ConsoleGUI;
using ConsoleGUI.Space;
using System.Text;
using Vezel.Cathode.Text.Control;

namespace Jumbee.Console.Snapshot;
/// <summary>
/// A stateful counterpart to <see cref="AnsiConsoleSnapshot"/> for testing the <em>live</em> render — used to
/// reproduce diff/cursor or async-ordering bugs that only appear across frames (e.g. press → release).
/// </summary>
/// <remarks>
/// It keeps <see cref="ConsoleManager"/> set up across multiple frames so each <see cref="FrameAsync"/> emits an
/// incremental diff against the persistent buffer (not a fresh full frame), and folds those bytes into a
/// persistent <see cref="Screen"/> — exactly as a real terminal accumulates them. Dispose to restore the sink.
/// </remarks>
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

    #endregion Constructors

    #region Properties

    /// <summary>The accumulated screen after every frame folded in so far.</summary>
    public AnsiScreen Screen { get; }

    #endregion Properties

    #region Methods

    /// <summary>Sets up the console for <paramref name="content"/> and folds in the initial frame.</summary>
    public static async Task<AnsiConsoleSession> StartAsync(IControl content, int width, int height)
    {
        var session = new AnsiConsoleSession(width, height);
        ConsoleManager.Console = new HeadlessConsole { Size = new Size(width, height) };
        ConsoleManager.Setup();
        // Bind a layout's wrapped control, never the layout proxy: a DrawingContext identity-checks the control
        // bubbling damage against its Child, and a Layout's wrapped ConsoleGUI control bubbles as itself — so
        // binding the proxy silently drops every damage report from the layout's children. The first frame still
        // paints (it is a full redraw), which makes the failure look like "live updates just don't work". UI.Start
        // and ConsoleSnapshot.Render both unwrap for the same reason.
        ConsoleManager.Content = content is ILayout layout ? layout.CControl : content;
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

    /// <summary>Restores the previous <see cref="ConsoleManager"/> output sink and ANSI mode.</summary>
    public void Dispose()
    {
        ConsoleManager.AnsiOutput = _prevOutput;
        ConsoleManager.AnsiEnabled = _prevAnsi;
    }

    #endregion Methods

    #region Fields

    private readonly StringBuilder _capture = new();
    private readonly Func<AnsiControlSequenceBuilder, Task> _prevOutput;
    private readonly bool _prevAnsi;

    #endregion Fields
}