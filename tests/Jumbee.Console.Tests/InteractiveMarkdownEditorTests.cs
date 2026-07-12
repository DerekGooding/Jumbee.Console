namespace Jumbee.Console.Tests;

using ConsoleGUI.Input;

using Jumbee.Console;
using Jumbee.Console.Snapshot;

using Xunit;

// The editor composes core controls; these tests exercise the live-sync behaviour that
// --verify (a one-frame render check) can't: an edit in the code pane must flow to the preview pane, and caret-only
// movement must not. Headless (UI not running), so the coalesced sync applies inline (see ScheduleSync).
public class InteractiveMarkdownEditorTests
{
    private static ConsoleKeyInfo K(char ch, ConsoleKey key) => new(ch, key, false, false, false);

    [Fact]
    public void Construction_StartsBothPanesInSync()
    {
        var ed = new InteractiveMarkdownEditor("# Title\n\nbody");

        Assert.Equal("# Title\n\nbody", ed.Editor.Text);
        Assert.Equal("# Title\n\nbody", ed.Preview.Markdown);
    }

    [Fact]
    public void Editing_TheCodePane_UpdatesThePreview_AndRaisesTextChanged()
    {
        var ed = new InteractiveMarkdownEditor("# Doc\n");
        ConsoleSnapshot.Render(ed, 80, 20);

        string? fired = null;
        ed.TextChanged += t => fired = t;

        // Paste is the simplest deterministic edit; it raises the editor's Changed just like typing.
        ed.Editor.Editor.OnPaste("more");

        Assert.Equal(ed.Editor.Text, ed.Preview.Markdown);   // preview mirrors the editor
        Assert.Contains("more", ed.Preview.Markdown);
        Assert.Equal(ed.Editor.Text, fired);                 // TextChanged carried the new text
    }

    [Fact]
    public void SettingText_LoadsTheEditor_AndRefreshesThePreview()
    {
        var ed = new InteractiveMarkdownEditor();

        ed.Text = "| a | b |\n|---|---|\n| 1 | 2 |";

        Assert.Equal(ed.Text, ed.Preview.Markdown);
        Assert.Equal(ed.Editor.Text, ed.Preview.Markdown);
    }

    [Fact]
    public void FramedEditor_AsFillUnderTopDockedSelect_RendersBothPanes()
    {
        // Reproduces the "Interactive Markdown" example layout: a one-row Select toolbar docked on top of the editor in
        // the DockPanel fill slot. Two things this guards: (1) the editor sets FillsFrameViewport, so it needs a
        // (borderless) frame to size to the fill area; (2) the docked toolbar must be intrinsically 1 tall — a Select
        // is (Height=1), whereas a HorizontalStackPanel resizes to the full height and would collapse the fill region.
        var editor = new InteractiveMarkdownEditor("# Hello\n\nsome body text");
        editor.WithFrame(borderStyle: BorderStyle.None);
        var toolbar = new Select("Editor 30%", "Even 50%", "Editor 70%") { Placeholder = "Layout" };
        var dock = new DockPanel(DockedControlPlacement.Top, toolbar, editor);

        var text = ConsoleSnapshot.ToText(dock, 100, 24);

        Assert.Contains("Layout", text);      // dropdown toolbar drew
        Assert.Contains("Markdown", text);    // left pane frame title
        Assert.Contains("Preview", text);     // right pane frame title
        Assert.Contains("Hello", text);       // editor content actually rendered into the fill area
    }

    [Fact]
    public void CaretOnlyMovement_DoesNotRaiseTextChanged()
    {
        // The wrapped TextEditor raises Changed on caret moves too; the editor must suppress those from the preview /
        // TextChanged path (only real text changes propagate).
        var ed = new InteractiveMarkdownEditor("abc\ndef");
        ConsoleSnapshot.Render(ed, 80, 20);
        UI.SetFocus(ed.Editor.Editor);

        var count = 0;
        ed.TextChanged += _ => count++;

        UI.SendInput(ed.Editor.Editor, K('\0', ConsoleKey.RightArrow));
        UI.SendInput(ed.Editor.Editor, K('\0', ConsoleKey.DownArrow));

        Assert.Equal(0, count);
        Assert.Equal(ed.Editor.Text, ed.Preview.Markdown);   // still in sync, unchanged
    }
}
