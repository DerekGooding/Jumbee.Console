namespace Jumbee.Console.Tests;

using System.IO;

using Jumbee.Console;

using Xunit;

// AlternateScreen must emit the DEC 1049 enter on construction and, on dispose, reset SGR + show the cursor + leave
// the alternate screen — restoring the user's primary screen. Dispose is idempotent (Stop and the ProcessExit net can
// both fire).
public class AlternateScreenTests
{
    [Fact]
    public void Enter_WritesAltScreenEnter_Dispose_WritesResetShowCursorAndLeave()
    {
        var orig = System.Console.Out;
        var sw = new StringWriter();
        System.Console.SetOut(sw);
        try
        {
            var screen = AlternateScreen.Enter();
            Assert.Equal("\x1b[?1049h", sw.ToString());   // entered the alternate buffer, nothing else yet

            screen.Dispose();
            // Reset graphics rendition, show the cursor, then switch back to the primary screen.
            Assert.Equal("\x1b[?1049h\x1b[0m\x1b[?25h\x1b[?1049l", sw.ToString());

            var afterFirstDispose = sw.ToString();
            screen.Dispose();   // idempotent — a second dispose (e.g. from the ProcessExit hook) writes nothing more
            Assert.Equal(afterFirstDispose, sw.ToString());
        }
        finally
        {
            System.Console.SetOut(orig);
        }
    }
}
