namespace Jumbee.Console.Tests;

using Jumbee.Console;
using Jumbee.Console.Snapshot;

using Xunit;

public class MultiTabCodeEditorTests
{
    [Fact]
    public void OpenDocument_AddsTab_SelectsIt_AndReturnsTheEditor()
    {
        var group = new MultiTabCodeEditor(Language.CSharp);

        var editor = group.OpenDocument("main.cs", "class C { }");

        Assert.Equal(1, group.DocumentCount);
        Assert.Same(editor, group.ActiveEditor);
        Assert.Equal("main.cs", group.ActiveDocumentName);
        Assert.Equal("class C { }", editor.Text);
    }

    [Fact]
    public void ActiveEditor_TracksSelection()
    {
        var group = new MultiTabCodeEditor();
        var a = group.OpenDocument("a.txt", "aaa");
        var b = group.OpenDocument("b.txt", "bbb");   // opening selects the latest

        Assert.Same(b, group.ActiveEditor);

        group.Tabs.SelectedIndex = 0;
        Assert.Same(a, group.ActiveEditor);
    }

    [Fact]
    public void NewDocument_OpensAnUntitledEditor()
    {
        var group = new MultiTabCodeEditor();

        group.NewDocument();
        group.NewDocument();

        Assert.Equal(2, group.DocumentCount);
        Assert.Equal("untitled-2", group.ActiveDocumentName);
    }

    [Fact]
    public void CloseDocument_RemovesTab_AndRaisesClosed()
    {
        var group = new MultiTabCodeEditor();
        var a = group.OpenDocument("a", "1");
        var b = group.OpenDocument("b", "2");
        CodeEditor? closed = null;
        group.DocumentClosed += e => closed = e;

        group.CloseDocument(b);

        Assert.Equal(1, group.DocumentCount);
        Assert.Same(b, closed);
        Assert.Same(a, group.ActiveEditor);
    }

    [Fact]
    public void DocumentClosing_CanCancel_KeepingTheDocument()
    {
        var group = new MultiTabCodeEditor();
        var a = group.OpenDocument("a", "1");
        group.DocumentClosing += (_, e) => e.Cancel = true;

        group.CloseDocument(a);

        Assert.Equal(1, group.DocumentCount);
        Assert.Same(a, group.ActiveEditor);
    }

    [Fact]
    public void SetDirty_TogglesAMarkerOnTheTabLabel()
    {
        var group = new MultiTabCodeEditor();
        var a = group.OpenDocument("a.cs", "x");

        group.SetDirty(a, true);
        Assert.StartsWith("● ", group.ActiveDocumentName);

        group.SetDirty(a, false);
        Assert.Equal("a.cs", group.ActiveDocumentName);
    }

    [Fact]
    public void RendersActiveEditorContent_AndTabBar()
    {
        var group = new MultiTabCodeEditor();
        group.OpenDocument("readme", "hello world");

        var text = ConsoleSnapshot.ToText(group, 40, 8);

        Assert.Contains("readme", text);        // tab label in the bar
        Assert.Contains("hello world", text);   // active editor content
    }
}
