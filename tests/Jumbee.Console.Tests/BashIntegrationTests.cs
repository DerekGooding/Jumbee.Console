namespace Jumbee.Console.Tests;

using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Jumbee.Console;

using Xunit;

/// <summary>
/// INTEGRATION tests that spawn a REAL shell in a REAL pseudo terminal (<see cref="Pty.Start"/> → ConPTY on
/// Windows / posix_spawn pty on Unix) and assert on its captured terminal output. Cross-platform: uses Git Bash's
/// <c>bash.exe</c> on Windows and <c>/bin/bash</c> on Unix; each test SKIPS (returns) when no bash is found.
///
/// These exercise the full transport + real program behavior — unlike the headless unit tests, which drive the
/// emulator with hand-written escape sequences. They are slower and environment-dependent, so they carry the
/// <c>Integration</c> trait: run them with <c>dotnet test --filter Category=Integration</c>, and the fast unit
/// suite with <c>--filter Category!=Integration</c>. Every read is bounded by a timeout so a misbehaving
/// environment can never hang the suite. (Authored on Windows; validate on a machine with Git Bash / a real
/// console — ConPTY does not run in a console-less sandbox.)
/// </summary>
[Trait("Category", "Integration")]
public class BashIntegrationTests
{
    [Fact]
    public async Task Bash_RunsCommand_AndWeCaptureItsOutput()
    {
        var bash = ResolveBash();
        if (bash is null) return;   // skip: no bash on this machine

        using var pty = Pty.Start($"{Quote(bash)} --norc --noprofile -i", 80, 25);
        Send(pty, "echo HELLO_JUMBEE\n");
        Send(pty, "exit\n");

        var output = await ReadOutputAsync(pty, TimeSpan.FromSeconds(10));
        Assert.Contains("HELLO_JUMBEE", output);
    }

    [Fact]
    public async Task Bash_FloodThenMarker_DrainsWithoutHanging()
    {
        var bash = ResolveBash();
        if (bash is null) return;

        using var pty = Pty.Start($"{Quote(bash)} --norc --noprofile -i", 80, 25);
        Send(pty, "yes | head -100000\n");   // a flood, bounded by head
        Send(pty, "echo DONE_MARKER\n");      // reaching this proves the flood drained, not hung
        Send(pty, "exit\n");

        var output = await ReadOutputAsync(pty, TimeSpan.FromSeconds(20));
        Assert.Contains("DONE_MARKER", output);
    }

    [Fact]
    public async Task Bash_AnsiColor_AppearsInOutputStream()
    {
        var bash = ResolveBash();
        if (bash is null) return;

        using var pty = Pty.Start($"{Quote(bash)} --norc --noprofile -i", 80, 25);
        Send(pty, "printf 'X\\033[31mRED\\033[0m\\n'\n");   // emit an SGR colour sequence
        Send(pty, "exit\n");

        var output = await ReadOutputAsync(pty, TimeSpan.FromSeconds(10));
        Assert.Contains("\x1b[31m", output);   // the raw colour escape made it through the PTY
        Assert.Contains("RED", output);
    }

    #region Helpers
    private static void Send(IPty pty, string s)
    {
        var b = Encoding.UTF8.GetBytes(s);
        pty.Input.Write(b, 0, b.Length);
        pty.Input.Flush();
    }

    // Reads the child's output until EOF (process exit) or the timeout, returning what was captured.
    private static async Task<string> ReadOutputAsync(IPty pty, TimeSpan timeout)
    {
        var sb = new StringBuilder();
        var buf = new byte[4096];
        using var cts = new CancellationTokenSource(timeout);
        try
        {
            while (true)
            {
                var n = await pty.Output.ReadAsync(buf.AsMemory(), cts.Token);
                if (n <= 0) break;
                sb.Append(Encoding.UTF8.GetString(buf, 0, n));
            }
        }
        catch (OperationCanceledException) { /* timed out — return what we have */ }
        catch (Exception) { /* pipe closed on exit */ }
        return sb.ToString();
    }

    // A bash to drive: Git Bash on Windows, the system bash on Unix. Null when none is installed.
    private static string? ResolveBash()
    {
        string[] candidates = OperatingSystem.IsWindows()
            ? [@"C:\Program Files\Git\bin\bash.exe", @"C:\Program Files\Git\usr\bin\bash.exe",
               @"C:\Program Files (x86)\Git\bin\bash.exe"]
            : ["/bin/bash", "/usr/bin/bash", "/usr/local/bin/bash"];

        foreach (var c in candidates)
            if (File.Exists(c)) return c;
        return null;
    }

    private static string Quote(string path) => path.Contains(' ') ? $"\"{path}\"" : path;
    #endregion
}
