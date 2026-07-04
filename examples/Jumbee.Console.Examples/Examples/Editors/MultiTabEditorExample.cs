namespace Jumbee.Console.Examples;

/// <summary>The MultiTabCodeEditor itself — a tabbed group of syntax-highlighting code editors with closable tabs
/// (✕), a "+" to add one, and per-editor scrolling. (The read-only Source pane on the right is the same control.)</summary>
public sealed class MultiTabEditorExample : ExampleBase
{
    public override string Category => "Editors";
    public override string Title => "Tabbed Code Editor";
    public override string Description =>
        "A VS-Code-style editor group: closable tabs, a + to open new documents, syntax highlighting, line numbers and independent scrolling.";

    public override IFocusable Build()
    {
        var group = new MultiTabCodeEditor(Language.CSharp);
        group.OpenDocument("Program.cs",
            "static class Program\n{\n    static void Main()\n    {\n        System.Console.WriteLine(\"Hello, Jumbee!\");\n    }\n}\n");
        group.OpenDocument("notes.md",
            "# Notes\n\n- Edit any tab\n- Click the ✕ to close a tab\n- Click the + to open a new one\n- Alt+←/→ switches tabs\n",
            Language.Markdown);
        return group;
    }
}
