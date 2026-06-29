namespace Jumbee.Console.Tests;

using System.Linq;
using System.Threading.Tasks;

using ConsoleGUI;

using Jumbee.Console.Snapshot;

using Xunit;

/// <summary>TextEditor through the real ConsoleManager ANSI render across frames (the cursor/diff layer that
/// ConsoleSnapshot can't see). TextEditor writes content via _Write (which brackets the cursor), so it does NOT
/// share TextInput's first-char-wipe bug — these lock that in.</summary>
public class TextEditorAnsiTests
{
    private static ConsoleKeyInfo Type(char c) => new(c, (ConsoleKey)char.ToUpperInvariant(c), false, false, false);
    private static ConsoleKeyInfo K(ConsoleKey k) => new('\0', k, false, false, false);
    private static string Row0(AnsiConsoleSession s, int w) =>
        string.Concat(Enumerable.Range(0, w).Select(x => s.Screen.Buffer[x, 0].Content ?? ' '));

    [Fact]
    public async Task FocusedEditor_KeepsFirstChar_AfterRewrite()
    {
        var ed = new TextEditor { IsFocused = true };
        using var session = await AnsiConsoleSession.StartAsync((IControl)ed.FocusableControl, 26, 3);

        foreach (var c in "cook") { UI.SendInput(ed, Type(c)); await session.FrameAsync(); }
        ed.Text = "Cookie";
        await session.FrameAsync();

        Assert.StartsWith("Cookie", Row0(session, 26));
    }

    [Fact]
    public async Task CaretNavigation_DoesNotCorruptText()
    {
        var ed = new TextEditor { IsFocused = true };
        using var session = await AnsiConsoleSession.StartAsync((IControl)ed.FocusableControl, 26, 3);

        ed.Text = "hello";
        await session.FrameAsync();
        UI.SendInput(ed, K(ConsoleKey.Home));   // navigation-only frame (no rewrite)
        await session.FrameAsync();

        Assert.StartsWith("hello", Row0(session, 26));
    }
}
