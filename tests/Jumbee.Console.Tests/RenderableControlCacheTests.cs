namespace Jumbee.Console.Tests;

using System.Collections.Generic;

using ConsoleGUI;
using ConsoleGUI.Common;
using ConsoleGUI.Input;
using ConsoleGUI.Space;

using Spectre.Console.Rendering;

using Xunit;

/// <summary>
/// Tests the retained-mode fast path in <see cref="RenderableControl"/>: the (expensive) Spectre render runs only
/// when content changes, not on every interactive-state change (focus / mouse). Controls that render interactive
/// state (the default) still re-render on those events.
/// </summary>
public class RenderableControlCacheTests
{
    // Counts how many times the Spectre pipeline (Render(options, maxWidth)) actually runs.
    private sealed class CountingRenderable(bool interactive) : RenderableControl
    {
        public int RenderCount { get; private set; }

        protected override bool RendersInteractiveState => interactive;

        protected override IEnumerable<Segment> Render(RenderOptions options, int maxWidth)
        {
            RenderCount++;
            return [new Segment("x")];
        }

        public void ChangeContent() => Invalidate();   // simulate a content change
    }

    private sealed class NoopListener : IDrawingContextListener
    {
        public void OnRedraw(DrawingContext drawingContext) { }
        public void OnUpdate(DrawingContext drawingContext, Rect rect) { }
    }

    private static DrawingContext Size(RenderableControl c, int w, int h)
    {
        var ctx = new DrawingContext(new NoopListener(), (IControl)c.FocusableControl);
        ctx.SetLimits(new Size(w, h), new Size(w, h));
        UI.PaintFrame();   // first paint establishes the cached render
        return ctx;
    }

    [Fact]
    public void ContentOnlyControl_SkipsRender_OnHover_ButRendersOnContentChange()
    {
        var c = new CountingRenderable(interactive: false);
        using var ctx = Size(c, 10, 1);

        var baseline = c.RenderCount;
        Assert.True(baseline >= 1, "the control should render at least once initially");

        // Hover (and release): a content-only control must reuse its cached buffer, not re-run the Spectre pipeline.
        ((IMouseListener)c).OnMouseEnter();
        UI.PaintFrame();
        ((IMouseListener)c).OnMouseLeave();
        UI.PaintFrame();
        Assert.Equal(baseline, c.RenderCount);

        // Focus change is likewise interactive-only -> still cached.
        c.IsFocused = true;
        UI.PaintFrame();
        Assert.Equal(baseline, c.RenderCount);

        // An actual content change must re-render.
        c.ChangeContent();
        UI.PaintFrame();
        Assert.Equal(baseline + 1, c.RenderCount);
    }

    [Fact]
    public void InteractiveControl_StillRenders_OnHover()
    {
        var c = new CountingRenderable(interactive: true);
        using var ctx = Size(c, 10, 1);

        var baseline = c.RenderCount;
        ((IMouseListener)c).OnMouseEnter();
        UI.PaintFrame();

        Assert.Equal(baseline + 1, c.RenderCount);   // hover re-renders when the control draws interactive state
    }
}
