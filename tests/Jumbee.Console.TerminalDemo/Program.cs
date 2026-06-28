namespace Jumbee.Console.TerminalDemo;

using System;
using System.IO;

using Jumbee.Console;

using static Jumbee.Console.Style;

/// <summary>
/// Dedicated demo for <see cref="TerminalEmulator"/>: runs a real shell in a pseudo-console, framed as a Jumbee
/// control. Type into it as you would any terminal — keystrokes are translated to terminal bytes (honoring the
/// emulator's app-cursor/keypad modes), and the shell's ANSI output is parsed and painted into the cell area.
///
/// Defaults to PowerShell 7 (<c>pwsh</c>), falling back to Windows PowerShell then <c>cmd</c>; pass a command line
/// as the first argument to override (e.g. <c>dotnet run -- cmd.exe</c>). Ctrl+Q quits the demo; typing <c>exit</c>
/// in the shell also closes it. Needs a VT terminal (e.g. Windows Terminal).
/// </summary>
public static class Program
{
    public static void Main(string[] args)
    {
        var shell = args.Length > 0 ? args[0] : ResolveShell();

        var term = new TerminalEmulator(shell);
        var baseTitle = $"{shell}  —  Ctrl+Q quits, or type 'exit'";
        term.WithRoundedBorder(Cyan1).WithTitle(baseTitle);

        // Reflect the program's OSC window title in the frame (falls back to the base title when cleared).
        term.TitleChanged += t => term.Frame!.Title = string.IsNullOrWhiteSpace(t) ? baseTitle : $"{t}  —  Ctrl+Q quits";

        var status = new TextLabel(TextLabelOrientation.Horizontal,
            $"TerminalEmulator demo · shell: {shell} · ConPTY + VtNetCore".PadRight(96), Color.Grey);

        // Quit the UI loop when the child process exits (e.g. the user typed 'exit').
        term.Exited += UI.Stop;

        var grid = new Jumbee.Console.Grid(
            [1, 28],   // status row, then the terminal fills the rest
            [98],
            [
                [status],
                [term],
            ]);

        var run = UI.Start(grid, width: 100, height: 31, isAnsiTerminal: true, input: new Jumbee.Console.VtInputSource());
        UI.SetFocus(term);
        run.Wait();

        // Ensure the child process and read loop are torn down on the way out (Ctrl+Q path).
        term.Dispose();
    }

    // First shell from the preference list that resolves on PATH (cmd.exe is the guaranteed fallback).
    private static string ResolveShell()
    {
        foreach (var candidate in new[] { "pwsh.exe", "powershell.exe", "cmd.exe" })
            if (OnPath(candidate)) return candidate;
        return "cmd.exe";
    }

    private static bool OnPath(string exe)
    {
        var path = Environment.GetEnvironmentVariable("PATH") ?? "";
        foreach (var dir in path.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
        {
            try { if (File.Exists(Path.Combine(dir.Trim(), exe))) return true; }
            catch { /* skip malformed PATH entries */ }
        }
        return false;
    }
}
