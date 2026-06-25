namespace Jumbee.Console.Tests;

using System;

using ConsoleGUI.Input;
using ConsoleGUI.Space;

using Jumbee.Console;

using Xunit;

public class MouseInputTests
{
    /// <summary>Exposes the protected mouse hooks/state of <see cref="Control"/> for assertions.</summary>
    private sealed class TestControl : Control
    {
        public int Clicks;
        public int DoubleClicks;
        public int Enters;
        public int Leaves;
        public bool Over => IsMouseOver;
        public bool Pressed => IsMousePressed;
        public int WheelRuns;
        public int WheelDelta;

        protected override void Render() { }
        protected override void OnClick(Position p) => Clicks++;
        protected override void OnDoubleClick(Position p) => DoubleClicks++;
        protected override void OnMouseEnter() => Enters++;
        protected override void OnMouseLeave() => Leaves++;
        protected override void OnMouseWheel(Position p, int delta) { WheelRuns++; WheelDelta = delta; }
    }

    /// <summary>A control that does NOT override the wheel hook, to exercise the default (scroll-frame) path.</summary>
    private sealed class BareControl : Control
    {
        protected override void Render() { }
    }

    private static readonly Position Origin = new(0, 0);

    [Fact]
    public void PressThenRelease_OnSameControl_SynthesizesClick()
    {
        var c = new TestControl();
        var m = (IMouseListener)c;

        m.OnMouseDown(Origin);
        Assert.True(c.Pressed);
        m.OnMouseUp(Origin);

        Assert.Equal(1, c.Clicks);
        Assert.False(c.Pressed);
    }

    [Fact]
    public void ReleaseWithoutPress_DoesNotClick()
    {
        var c = new TestControl();

        ((IMouseListener)c).OnMouseUp(Origin);

        Assert.Equal(0, c.Clicks);
    }

    [Fact]
    public void LeaveBetweenPressAndRelease_CancelsClick()
    {
        var c = new TestControl();
        var m = (IMouseListener)c;

        m.OnMouseDown(Origin);
        m.OnMouseLeave();      // pointer left while held -> press is cancelled
        m.OnMouseUp(Origin);

        Assert.Equal(0, c.Clicks);
        Assert.False(c.Pressed);
    }

    [Fact]
    public void Enter_And_Leave_TrackHoverState()
    {
        var c = new TestControl();
        var m = (IMouseListener)c;

        m.OnMouseEnter();
        Assert.True(c.Over);
        Assert.Equal(1, c.Enters);

        m.OnMouseLeave();
        Assert.False(c.Over);
        Assert.Equal(1, c.Leaves);
    }

    [Fact]
    public void TwoQuickClicks_AtSamePosition_RegisterDoubleClick()
    {
        var c = new TestControl();
        var m = (IMouseListener)c;

        m.OnMouseDown(Origin); m.OnMouseUp(Origin);   // first click
        m.OnMouseDown(Origin); m.OnMouseUp(Origin);   // second within threshold

        Assert.Equal(1, c.Clicks);          // only the first is a single click
        Assert.Equal(1, c.DoubleClicks);
    }

    [Fact]
    public void Button_Click_RaisesActivated()
    {
        var b = new Button("OK");
        var activated = 0;
        b.Activated += (_, _) => activated++;

        var m = (IMouseListener)b;
        m.OnMouseDown(Origin);
        m.OnMouseUp(Origin);

        Assert.Equal(1, activated);
    }

    [Theory]
    [InlineData(ConsoleKey.Enter)]
    [InlineData(ConsoleKey.Spacebar)]
    public void Button_EnterOrSpace_RaisesActivated(ConsoleKey key)
    {
        var b = new Button("OK");
        var activated = 0;
        b.Activated += (_, _) => activated++;

        UI.SendInput(b, key);

        Assert.Equal(1, activated);
    }

    [Fact]
    public void Button_OtherKey_DoesNotActivate()
    {
        var b = new Button("OK");
        var activated = 0;
        b.Activated += (_, _) => activated++;

        UI.SendInput(b, ConsoleKey.A);

        Assert.Equal(0, activated);
    }

    [Theory]
    [InlineData(-3)]   // wheel up
    [InlineData(3)]    // wheel down
    public void Wheel_RoutesDeltaToHook(int delta)
    {
        var c = new TestControl();

        ((IMouseWheelListener)c).OnMouseWheel(Origin, delta);

        Assert.Equal(1, c.WheelRuns);
        Assert.Equal(delta, c.WheelDelta);
    }

    [Fact]
    public void Wheel_RaisesMouseWheeledEvent()
    {
        var c = new TestControl();
        var received = 0;
        c.MouseWheeled += (_, d) => received = d;

        ((IMouseWheelListener)c).OnMouseWheel(Origin, 3);

        Assert.Equal(3, received);
    }

    [Fact]
    public void Wheel_DefaultPath_WithNoFrame_DoesNotThrow()
    {
        var c = new BareControl();   // uses the default OnMouseWheel -> Frame?.Scroll, Frame is null

        ((IMouseWheelListener)c).OnMouseWheel(Origin, 3);
    }
}
