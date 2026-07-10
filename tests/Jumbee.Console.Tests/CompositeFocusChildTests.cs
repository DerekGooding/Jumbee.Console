namespace Jumbee.Console.Tests;

using ConsoleGUI.Input;

using Jumbee.Console;
using Jumbee.Console.Snapshot;

using Xunit;

// Reproduces the examples-browser middle pane: a content-swapping composite whose live content is a framed control,
// with a focusable-but-non-input description label docked above it. The default CompositeControl focus child is the
// first focusable descendant it claimed — which is the label — so focus (and therefore keyboard input) never reaches
// the content. Overriding FocusChild to point at the current content fixes it.
public class CompositeFocusChildTests
{
    private sealed class PaneHost : CompositeControl
    {
        private readonly Control _content;
        private readonly bool _override;

        public PaneHost(Control content, bool overrideFocusChild)
        {
            _content = content;
            _override = overrideFocusChild;
            var header = new TextLabel(TextLabelOrientation.Horizontal, "description");   // focusable-by-default label
            content.WithFrame(borderStyle: BorderStyle.None);
            SetContent(new DockPanel(DockedControlPlacement.Top, header, content.Frame!));
        }

        protected internal override bool FillsFrameViewport => true;
        protected override Control? FocusChild => _override ? _content : base.FocusChild;
    }

    private static ConsoleKeyInfo Down => new('\0', ConsoleKey.DownArrow, false, false, false);

    // Builds root(DockPanel) > SplitPanel > host(composite) > DockPanel(label, Framed(list)), like the browser shell.
    private static (ListBox list, ILayout root) BuildPane(bool overrideFocusChild)
    {
        var list = new ListBox("alpha", "beta", "gamma");
        var host = new PaneHost(list, overrideFocusChild);
        host.WithFrame(title: "Example");
        var split = new SplitPanel(SplitOrientation.Horizontal, host, new ListBox("other"), splitPosition: 20);
        var status = new TextLabel(TextLabelOrientation.Horizontal, "status");
        var root = new DockPanel(DockedControlPlacement.Bottom, status, split);
        ConsoleSnapshot.Render(root, 40, 12);   // establish layout + register controls for focus
        return (list, root);
    }

    [Fact]
    public void FocusableHeader_StealsCompositeFocus_SoArrowKeysMissTheContent()
    {
        var (list, root) = BuildPane(overrideFocusChild: false);

        list.Focus();   // click-to-focus resolves to the host, which delegates to its (wrong) focus child
        root.OnInput(new UI.InputEventArgs(new InputEvent(Down)));

        Assert.Equal(0, list.SelectedIndex);   // the key never reached the list — the documented bug
    }

    [Fact]
    public void FocusChildOverride_RoutesArrowKeysToTheContent()
    {
        var (list, root) = BuildPane(overrideFocusChild: true);

        list.Focus();
        root.OnInput(new UI.InputEventArgs(new InputEvent(Down)));

        Assert.Equal(1, list.SelectedIndex);   // DownArrow moved the selection — keyboard navigation works
    }
}
