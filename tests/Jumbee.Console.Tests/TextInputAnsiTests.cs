namespace Jumbee.Console.Tests;

using System.Linq;
using System.Threading.Tasks;

using ConsoleGUI;

using Jumbee.Console.Snapshot;

using Xunit;

/// <summary>Tests TextInput through the REAL ConsoleManager ANSI render across frames (what ConsoleSnapshot can't
/// see). Regression: the focused caret used to wipe the first character when the text was rewritten each frame,
/// because the buffer-cursor save/restore restored a stale cell.</summary>
public class TextInputAnsiTests
{
    private static ConsoleKeyInfo Type(char c) => new(c, (ConsoleKey)char.ToUpperInvariant(c), false, false, false);

    private static string Row0(AnsiConsoleSession s, int width) =>
        string.Concat(Enumerable.Range(0, width).Select(x => s.Screen.Buffer[x, 0].Content ?? ' '));

    [Fact]
    public async Task FocusedField_DoesNotDropFirstChar_AfterRewrite()
    {
        var input = new TextInput { IsFocused = true };
        using var session = await AnsiConsoleSession.StartAsync((IControl)input.FocusableControl, 26, 1);

        foreach (var c in "cook") { UI.SendInput(input, Type(c)); await session.FrameAsync(); }
        input.Text = "Cookie";   // accept a suggestion -> rewrites the line
        await session.FrameAsync();

        Assert.StartsWith("Cookie", Row0(session, 26));   // 'C' must survive the caret move (was blanked)
    }
}
