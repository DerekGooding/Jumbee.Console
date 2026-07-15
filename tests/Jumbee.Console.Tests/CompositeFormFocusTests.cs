namespace Jumbee.Console.Tests;

using ConsoleGUI.Input;
using ConsoleGUI.Space;

using Jumbee.Console;
using Jumbee.Console.Snapshot;

using Xunit;

// A form-like composite: several equally focusable fields the user clicks and tabs between (the Form Controls
// example). Focusing a child resolves to the composite, so the composite decides which child ends up focused —
// these pin that it follows the click, and that Tab walks the fields only when the composite opts in.
public class CompositeFormFocusTests
{
    private sealed class Form(bool tabNavigates) : CompositeControl
    {
        public TextInput Left { get; } = new TextInput("LEFT");
        public TextInput Right { get; } = new TextInput("RIGHT");

        public void Build() => SetContent(new Grid([1], [10, 10], [[Left, Right]]));

        protected override bool TabNavigatesChildren => tabNavigates;
    }

    private static ConsoleKeyInfo Tab(bool shift = false) => new('\t', ConsoleKey.Tab, shift, false, false);

    // Lays the form out inside a root layout, framed the way ExampleHost hosts an example — the frame is a separate
    // input-routing path from a bare composite in a layout, and both must run the composite's tunnel.
    private static (Form form, ILayout root) Build(bool tabNavigates, bool framed)
    {
        var form = new Form(tabNavigates);
        form.Build();
        if (framed) form.WithFrame(borderStyle: BorderStyle.Rounded);
        var root = new Grid([framed ? 3 : 1], [22], [[form.FocusableControl]]);
        ConsoleSnapshot.Render(root, 22, framed ? 3 : 1);
        return (form, root);
    }

    private static void Click(Control root, int x, int y)
    {
        var listener = root[new Position(x, y)].MouseListener!.Value;
        listener.MouseListener.OnMouseDown(listener.RelativePosition);
        listener.MouseListener.OnMouseUp(listener.RelativePosition);
    }

    [Fact]
    public void Click_FocusesTheClickedField_NotTheFirstOne()
    {
        var (form, _) = Build(tabNavigates: false, framed: false);
        form.IsFocused = true;
        Assert.Same(form.Left, form.FocusedControl);   // the composite starts on its first field

        Click(form, 12, 0);
        Assert.Same(form.Right, form.FocusedControl);

        Click(form, 2, 0);
        Assert.Same(form.Left, form.FocusedControl);
    }

    [Fact]
    public void Tab_MovesBetweenFields_WhenTheCompositeOptsIn()
    {
        var (form, root) = Build(tabNavigates: true, framed: false);
        form.IsFocused = true;

        root.OnInput(new UI.InputEventArgs(new InputEvent(Tab())));
        Assert.Same(form.Right, form.FocusedControl);

        root.OnInput(new UI.InputEventArgs(new InputEvent(Tab())));
        Assert.Same(form.Left, form.FocusedControl);   // wraps

        root.OnInput(new UI.InputEventArgs(new InputEvent(Tab(shift: true))));
        Assert.Same(form.Right, form.FocusedControl);   // Shift+Tab goes back
    }

    [Fact]
    public void Tab_MovesBetweenFields_WhenTheCompositeIsFramed()
    {
        var (form, root) = Build(tabNavigates: true, framed: true);
        form.IsFocused = true;

        root.OnInput(new UI.InputEventArgs(new InputEvent(Tab())));

        Assert.Same(form.Right, form.FocusedControl);
    }

    [Fact]
    public void Tab_ReachesTheFocusedField_WhenTheCompositeDoesNotOptIn()
    {
        var (form, root) = Build(tabNavigates: false, framed: false);
        form.IsFocused = true;

        root.OnInput(new UI.InputEventArgs(new InputEvent(Tab())));

        Assert.Same(form.Left, form.FocusedControl);   // Tab still belongs to the focused control by default
    }

    [Fact]
    public void Typing_StillReachesTheFocusedField_WithTabNavigationOn()
    {
        var (form, root) = Build(tabNavigates: true, framed: false);
        form.IsFocused = true;
        root.OnInput(new UI.InputEventArgs(new InputEvent(Tab())));   // -> Right

        root.OnInput(new UI.InputEventArgs(new InputEvent(new ConsoleKeyInfo('x', ConsoleKey.X, false, false, false))));

        Assert.Equal("RIGHTx", form.Right.Text);
        Assert.Equal("LEFT", form.Left.Text);
    }
}
