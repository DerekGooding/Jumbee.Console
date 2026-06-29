namespace Jumbee.Console.Tests;

using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ConsoleGUI;

using Jumbee.Console.Snapshot;

using Xunit;

/// <summary>TerminalEmulator through the real ConsoleManager ANSI render: output renders and the focused terminal
/// shows the cursor (it draws IsCursor directly, so it's structurally immune to the BufferCursor save/restore bug).</summary>
public class TerminalEmulatorAnsiTests
{
    private static string Row0(AnsiConsoleSession s, int w) =>
        string.Concat(Enumerable.Range(0, w).Select(x => s.Screen.Buffer[x, 0].Content ?? ' '));

    [Fact]
    public async Task FedOutput_RendersThroughAnsi_WithCursorWhenFocused()
    {
        var term = new TerminalEmulator(commandLine: null) { IsFocused = true };   // manual-drive
        using var session = await AnsiConsoleSession.StartAsync((IControl)term.FocusableControl, 24, 4);

        term.Feed(Encoding.ASCII.GetBytes("hello"));
        await session.FrameAsync();

        Assert.StartsWith("hello", Row0(session, 24));
        Assert.True(session.Screen.CursorVisible);   // focused terminal shows the caret
    }
}
