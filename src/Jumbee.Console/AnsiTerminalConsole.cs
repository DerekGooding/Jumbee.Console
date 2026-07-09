namespace Jumbee.Console;

using System;

using ConsoleGUI.Api;
using ConsoleGUI.Data;
using ConsoleGUI.Space;

/// <summary>
/// The <see cref="IConsole"/> used by the ANSI render path. The terminal size is <b>read</b> from the live terminal,
/// but the setter deliberately does <b>not</b> manipulate it — unlike <see cref="StandardConsole"/>, whose
/// <c>SetWindowSize</c>/<c>SetBufferSize</c> dance fights the live window size and never converges, so
/// <see cref="ConsoleManager.AdjustBufferSize"/> resizes (and re-lays-out the whole UI) on <em>every</em> frame.
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
        get { try { return _inner.Size; } catch { return _requested; } }
        set => _requested = value;   // remember it as the fallback; never resize the physical terminal
    }

    public bool KeyAvailable => false;   // input arrives via the VT input source, not Console.ReadKey
    #endregion

    #region Methods
    public void Initialize() => _inner.Initialize();   // UTF-8 + hide-cursor + clear, on genuine (re)init only
    public void OnRefresh() => _inner.OnRefresh();
    public void Write(Position position, in Character character) { }   // unused on the ANSI path (acsb writes directly)
    public ConsoleKeyInfo ReadKey() => throw new NotSupportedException();
    #endregion

    #region Fields
    private readonly StandardConsole _inner = new StandardConsole();
    private Size _requested = new Size(80, 25);
    #endregion
}
