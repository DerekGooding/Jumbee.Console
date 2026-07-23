
using System;

namespace Jumbee.Console;
/// <summary>
/// Switches the terminal to the alternate screen buffer (DEC private mode 1049) for the duration of a full-screen UI
/// session, so the app's frames never touch the primary screen or its scrollback. <see cref="Dispose"/> resets the
/// graphics rendition, shows the cursor and switches back — restoring whatever the user's terminal showed before the
/// app ran. A <see cref="AppDomain.ProcessExit"/> hook restores on an abrupt exit (crash / unhandled exception) so the
/// user is never stranded on a frozen alternate screen.
/// </summary>
/// <remarks>
/// Owned and sequenced by <see cref="UI"/>: entered in <see cref="UI.Start"/> before the first frame, and disposed in
/// <see cref="UI.Stop"/> only after the render loop has stopped and pending frame writes have drained — so leaving the
/// alternate screen is the last write to stdout and no frame can repaint the primary screen behind it. The escape
/// sequences go through <see cref="Console.Out"/>, matching <c>TerminalInputMode</c>.
/// </remarks>
internal sealed class AlternateScreen : IDisposable
{
    #region Methods

    public static AlternateScreen Enter() => new();

    private AlternateScreen()
    {
        System.Console.Out.Write(EnterSeq);
        System.Console.Out.Flush();
        AppDomain.CurrentDomain.ProcessExit += OnProcessExit;
    }

    private void OnProcessExit(object? sender, EventArgs e) => Dispose();

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        AppDomain.CurrentDomain.ProcessExit -= OnProcessExit;
        try { System.Console.Out.Write(LeaveSeq); System.Console.Out.Flush(); }
        catch { /* best effort */ }
    }

    #endregion Methods

    #region Fields

    private const string EnterSeq = "\x1b[?1049h";                 // switch to the alternate screen buffer
    private const string LeaveSeq = "\x1b[0m\x1b[?25h\x1b[?1049l"; // reset SGR, show cursor, switch back to primary
    private bool _disposed;

    #endregion Fields
}