namespace Jumbee.Console.Examples;

/// <summary>
/// A tabbed group of syntax-highlighting code editors — it <em>is</em> a <see cref="MultiTabCodeEditor"/>,
/// with closable tabs (✕), a "+" to add one, and per-editor scrolling.
/// </summary>
public sealed class MultiTabEditorExample : MultiTabCodeEditor, IExample
{
    public MultiTabEditorExample() : base(Language.CSharp)
    {
        OpenDocument("Program.cs",
            "static class Program\n{\n    static void Main()\n    {\n        System.Console.WriteLine(\"Hello, Jumbee!\");\n    }\n}\n");
        OpenDocument("notes.md",
            "# Notes\n\n- Edit any tab\n- Click the ✕ to close a tab\n- Click the + to open a new one\n- Alt+←/→ switches tabs\n",
            Language.Markdown);
    }

    #region IExample
    string IExample.Category => "Editors and Viewers";
    string IExample.Title => "Tabbed Code Editor";
    string IExample.Description =>
        "A VS-Code-style editor group: closable tabs, a + for new documents, syntax highlighting and independent scrolling.";
    #endregion
}
