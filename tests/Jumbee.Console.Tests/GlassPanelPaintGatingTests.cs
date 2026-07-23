namespace Jumbee.Console.Tests;

using Jumbee.Console;

using Xunit;

/// <summary>A <see cref="GlassPanel"/> (the base of <see cref="PerfHud"/>) only renders when it has a pending paint
/// request — so an un-shown / clean HUD does NOT re-render every frame. This confirms the perf HUD costs nothing while
/// hidden; a <c>GlassPanel.Render</c> showing up in a profile means it is currently toggled ON (glass over live
/// content re-blends each frame during compositing, which is inherent to a translucent overlay).</summary>
public class GlassPanelPaintGatingTests
{
    [Fact]
    public void CleanGlassPanel_DoesNotRenderEveryFrame()
    {
        var g = new CountingGlass();
        try
        {
            UI.PaintFrame();                 // first frame may render once (initial invalidation)
            var baseline = g.Renders;

            UI.PaintFrame();
            UI.PaintFrame();
            UI.PaintFrame();
            Assert.Equal(baseline, g.Renders);   // no further renders without an invalidation — hidden HUD is free
        }
        finally
        {
            g.Dispose();                     // unsubscribes its OnPaint from UI.Paint
        }
    }

    private sealed class CountingGlass : GlassPanel
    {
        public int Renders;
        public CountingGlass() : base(10, 3, new Color(20, 20, 30)) { }
        protected override void Render() { Renders++; base.Render(); }
    }
}
