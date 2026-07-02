namespace Jumbee.Console.Tests;

using Jumbee.Console;
using Jumbee.Console.Snapshot;

using Xunit;

/// <summary>Guards the fast bulk-paste path: a bracketed-paste payload routed through the layout must reach the
/// editor's single <see cref="TextEditor.OnPaste"/> (one insert), not the per-character <see cref="Control.OnPaste"/>
/// fallback — even when the editor is wrapped in a composite / frame / tab panel. (A single Changed event = one bulk
/// insert; N events would mean it degraded to per-character replay.)</summary>
public class PasteRoutingTests
{
    private static int CountChanged(TextEditor ed, System.Action paste)
    {
        var n = 0;
        void Handler(object? _, System.EventArgs __) => n++;
        ed.Changed += Handler;
        paste();
        ed.Changed -= Handler;
        return n;
    }

    [Fact]
    public void BareTextEditor_InGrid_BulkInserts()
    {
        var ed = new TextEditor();
        var grid = new Grid([10], [30], [[ed]]);
        ConsoleSnapshot.Render(grid, 32, 12);
        ed.Focus();

        var changed = CountChanged(ed, () => grid.OnPaste("abcdef"));   // the live path: UI routes PasteInputEvent -> layout.OnPaste

        Assert.Equal(1, changed);         // one bulk insert, not six per-char events
        Assert.Equal("abcdef", ed.Text);
    }

    [Fact]
    public void FramedCodeEditor_InGrid_BulkInserts()
    {
        var ce = new CodeEditor();
        ce.WithRoundedBorder();
        var grid = new Grid([10], [30], [[ce]]);
        ConsoleSnapshot.Render(grid, 32, 12);
        ce.Editor.Focus();

        var changed = CountChanged(ce.Editor, () => grid.OnPaste("abcdef"));

        Assert.Equal(1, changed);
        Assert.Equal("abcdef", ce.Text);
    }

    [Fact]
    public void CodeEditor_InTabPanel_BulkInserts()
    {
        var ce = new CodeEditor();
        var tabs = new TabPanel(TabBarDock.Top, ("Body", ce.WithFrame()));
        ConsoleSnapshot.Render(tabs, 32, 12);
        ce.Editor.Focus();

        var changed = CountChanged(ce.Editor, () => tabs.OnPaste("abcdef"));

        Assert.Equal(1, changed);
        Assert.Equal("abcdef", ce.Text);
    }
}
