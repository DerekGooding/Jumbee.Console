namespace Jumbee.Console;

/// <summary>
/// Factory that opens an <see cref="IPty"/> using the right backend for the current OS: the Windows pseudo console
/// (<see cref="ConPty"/>) or a Unix pseudo terminal (<see cref="UnixPty"/>).
/// </summary>
public static class Pty
{
    /// <summary>Launches <paramref name="commandLine"/> in a new pseudo terminal of the given size. When
    /// <paramref name="workingDirectory"/> is non-null the child starts in that directory; otherwise it inherits the
    /// host process's current directory.</summary>
    public static IPty Start(string commandLine, short columns, short rows, string? workingDirectory = null)
    {
        if (OperatingSystem.IsWindows())
            return ConPty.Start(commandLine, columns, rows, workingDirectory);
        return OperatingSystem.IsLinux() || OperatingSystem.IsMacOS()
            ? (IPty)UnixPty.Start(commandLine, columns, rows, workingDirectory)
            : throw new PlatformNotSupportedException("A pseudo terminal requires Windows (ConPTY) or Linux/macOS.");
    }

    /// <summary>The OS default interactive shell: <c>cmd.exe</c> on Windows, <c>$SHELL</c> (or <c>/bin/bash</c>)
    /// on Unix. Handy as the command line for <see cref="TerminalEmulator"/> on a non-Windows host.</summary>
    public static string DefaultShell => OperatingSystem.IsWindows()
        ? "cmd.exe"
        : Environment.GetEnvironmentVariable("SHELL") is { Length: > 0 } shell ? shell : "/bin/bash";
}