namespace Jumbee.Console.Tests;

using Jumbee.Console;

using Xunit;

// Passive display controls (no keyboard/mouse interaction) are non-focusable by default, so they never sit in the
// tab order and can't be chosen as a composite's focus child (which would swallow a sibling's keyboard input).
public class PassiveControlFocusTests
{
    public static TheoryData<Control> PassiveControls => new()
    {
        new TextLabel(TextLabelOrientation.Horizontal, "label"),
        new TextPanel("panel"),
        new Digits("123"),
        new Sparkline(1, 2, 3),
        new Gauge(50),
        new BarChart(("a", 1, Color.Red)),
        new Spinner(),
        new Badge("badge"),
        new Footer(),
        new SpectreLiveDisplay(new Spectre.Console.Text("live")),
        new SpectreTaskProgress(),
    };

    [Theory]
    [MemberData(nameof(PassiveControls))]
    public void PassiveDisplayControl_IsNotFocusable(Control control) => Assert.False(control.Focusable);

    [Fact]
    public void InteractiveControl_StaysFocusable()
    {
        Assert.True(new ListBox("a").Focusable);   // sanity: the change didn't flip interactive controls
        Assert.True(new Button("ok").Focusable);
    }
}
