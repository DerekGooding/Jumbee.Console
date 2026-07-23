
using ConsoleGUI.Api;
using ConsoleGUI.Data;
using ConsoleGUI.Space;

namespace Jumbee.Console;
/// <summary>
/// The <see cref="IConsole"/> used by the ANSI render path. The terminal size is <b>read</b> from the live terminal,
/// but the setter deliberately does <b>not</b> manipulate it — unlike <see cref="StandardConsole"/>, whose
/// <c>SetWindowSize</c>/<c>SetBufferSize</c> dance fights the live window size and never converges, so
/// <c>ConsoleManager.AdjustBufferSize</c> resizes (and re-lays-out the whole UI) on <em>every</em> frame.
/// The ANSI renderer emits escape sequences into whatever size the terminal reports; the app adapts to the terminal,
/// not the reverse. Everything else (UTF-8 setup, cursor hide, clear) delegates to a real <see cref="StandardConsole"/>
/// and only runs on genuine (re)initialization. Rendering never uses <see cref="Write"/> on this path (the
/// <c>AnsiControlSequenceBuilder</c> writes directly), and input comes through the VT input source, not
/// <see cref="ReadKey"/>.
/// </summary>
internal sealed class AnsiTerminalConsole : IConsole
{
    #region Properties

    public Size Size
    {
        // Live terminal size; falls back to the last requested size if the terminal can't be queried (e.g. redirected).
        get { try { return _inner.Size; } catch { return field; } }
        set;   // remember it as the fallback; never resize the physical terminal
    } = new Size(80, 25);

    public bool KeyAvailable => false;   // input arrives via the VT input source, not Console.ReadKey

    #endregion Properties

    #region Methods

    public void Initialize()
    {
        // UTF-8 output, then hide the cursor and clear the screen via ANSI — deliberately NOT Console.Clear() /
        // Console.CursorVisible. On Windows those are Win32 console calls that act on the PRIMARY screen buffer even
        // while the alternate screen is active, so on exit (when we switch back from the alt screen) the user's prior
        // terminal output would be blanked instead of restored. `\x1b[2J` clears only the visible screen (scrollback
        // untouched); the renderer then repaints every cell, so no separate clear is needed for correctness.
        // Set UTF-8 only if it isn't already: setting Console.OutputEncoding recreates the Console.Out stream, and
        // Initialize runs on every resize — no need to churn the stream (or clobber a wrapping writer) each time.
        try { if (System.Console.OutputEncoding.CodePage != 65001) System.Console.OutputEncoding = System.Text.Encoding.UTF8; }
        catch { /* not a real console (redirected) */ }
        try { System.Console.Out.Write("\x1b[?25l\x1b[2J\x1b[H"); System.Console.Out.Flush(); } catch { /* best effort */ }
    }

    public void OnRefresh() => _inner.OnRefresh();

    public void Write(Position position, in Character character)
    { }   // unused on the ANSI path (acsb writes directly)

    public ConsoleKeyInfo ReadKey() => throw new NotSupportedException();

    #endregion Methods

    #region Fields

    private readonly StandardConsole _inner = new();

    #endregion Fields
}