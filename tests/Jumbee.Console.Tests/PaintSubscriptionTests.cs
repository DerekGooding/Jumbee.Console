namespace Jumbee.Console.Tests;

using Jumbee.Console;

using Xunit;

public class PaintSubscriptionTests
{
    // Subscribes a SECOND UI.Paint handler on top of the base Control.OnPaint (as PerfHud does with its refresh
    // handler). Before the fix, UI.Paint's add accessor dropped the second handler for an already-tracked control,
    // so the extra handler never fired and the HUD froze on its constructor's first render.
    private sealed class TwoHandlerControl : Control
    {
        public int Extra;
        public TwoHandlerControl() => UI.Paint += OnExtra;
        private void OnExtra(object? sender, UI.PaintEventArgs e) => Extra++;
        protected override void Render() { }
        public override void Dispose() { UI.Paint -= OnExtra; base.Dispose(); }
    }

    [Fact]
    public void Control_WithTwoPaintHandlers_BothFireEachFrame()
    {
        using var c = new TwoHandlerControl();
        var before = c.Extra;
        UI.PaintFrame();
        UI.PaintFrame();
        Assert.True(c.Extra >= before + 2, $"the control's second Paint handler should fire each frame (extra={c.Extra})");
    }
}
