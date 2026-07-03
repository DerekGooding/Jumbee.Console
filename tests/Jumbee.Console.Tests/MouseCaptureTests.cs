namespace Jumbee.Console.Tests;

using ConsoleGUI;
using ConsoleGUI.Input;
using ConsoleGUI.Space;

using Xunit;

public class MouseCaptureTests
{
    private sealed class Recorder : IMouseListener
    {
        public int Moves, Downs, Ups;
        public void OnMouseEnter() { }
        public void OnMouseLeave() { }
        public void OnMouseMove(Position position) => Moves++;
        public void OnMouseDown(Position position) => Downs++;
        public void OnMouseUp(Position position) => Ups++;
    }

    [Fact]
    public void CapturedListener_GetsMoveDownUp_RegardlessOfCell_ThenReleases()
    {
        var rec = new Recorder();
        try
        {
            ConsoleManager.CaptureMouse(rec);
            Assert.Same(rec, ConsoleManager.MouseCapture);

            // Move the pointer to arbitrary cells: while captured these route to rec, with no hit-test / no enter-leave.
            ConsoleManager.MousePosition = new Position(5, 5);
            ConsoleManager.MousePosition = new Position(40, 20);
            Assert.True(rec.Moves >= 1);

            ConsoleManager.MouseDown = true;
            ConsoleManager.MouseDown = false;
            Assert.Equal(1, rec.Downs);
            Assert.Equal(1, rec.Ups);

            ConsoleManager.ReleaseMouseCapture();
            Assert.Null(ConsoleManager.MouseCapture);

            // After release the listener no longer receives events.
            ConsoleManager.MousePosition = null;   // clears the hit-test context (no buffer access)
            var beforeDowns = rec.Downs;
            ConsoleManager.MouseDown = true;
            ConsoleManager.MouseDown = false;
            Assert.Equal(beforeDowns, rec.Downs);
        }
        finally
        {
            ConsoleManager.ReleaseMouseCapture();
            ConsoleManager.MouseDown = false;
            ConsoleManager.MousePosition = null;
        }
    }
}
