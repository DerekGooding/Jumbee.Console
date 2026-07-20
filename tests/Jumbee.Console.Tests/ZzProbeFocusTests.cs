namespace Jumbee.Console.Tests;

using Jumbee.Console;
using Jumbee.Console.Snapshot;

using Xunit;

// TEMPORARY probe — deleted after diagnosis.
public class ZzProbeFocusTests
{
    [Fact]
    public void Probe()
    {
        var path = @"C:\Projects\Jumbee.Console\artifacts\jc-focus-probe.txt";
        try
        {
            var list = new ListBox("a", "b", "c");
            list.Focus();
            var tint = UI.StyleTheme.Focus.BackgroundColor?.ToConsoleGUIColor();
            var ic = (ConsoleGUI.Api.IControl)((Control)list).FocusableControl;
            var buf = ConsoleSnapshot.Render(list, 10, 4);
            var msg =
                $"IsFocused={list.IsFocused}\n" +
                $"controlSize={ic.Size.Width}x{ic.Size.Height}\n" +
                $"bufSize={buf.Size.Width}x{buf.Size.Height}\n" +
                $"tint={tint}\n" +
                $"row0bg={buf[0, 0].Background}\n" +
                $"row3bg={buf[0, 3].Background}\n";
            System.IO.File.WriteAllText(path, msg);
        }
        catch (System.Exception ex)
        {
            System.IO.File.WriteAllText(path, "EXCEPTION: " + ex);
        }
    }
}
