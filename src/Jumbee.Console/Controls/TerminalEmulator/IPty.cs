namespace Jumbee.Console;
/// <summary>
/// A pseudo-terminal session: a child process attached to a PTY, exposing its stdin/stdout as streams.
/// </summary>
/// <remarks>
/// Implemented per OS — <see cref="ConPty"/> (Windows ConPTY) and <see cref="UnixPty"/> (Linux/macOS) — and created
/// through the <see cref="Pty.Start"/> factory. All implementations are pure managed P/Invoke (no shipped native
/// binaries).
/// </remarks>
public interface IPty : IDisposable
{
    /// <summary>Write here to send input (keystrokes/bytes) to the child process.</summary>
    Stream Input { get; }

    /// <summary>Read here to receive the child process's terminal output (the ANSI stream).</summary>
    Stream Output { get; }

    /// <summary>Resizes the pseudo terminal (call when the host control's cell area changes).</summary>
    void Resize(short columns, short rows);

    /// <summary>Raised (off the UI thread) when the child process exits.</summary>
    event Action? Exited;
}