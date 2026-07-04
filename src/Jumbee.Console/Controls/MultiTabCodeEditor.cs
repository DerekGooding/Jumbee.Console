namespace Jumbee.Console;

using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// A tabbed group of <see cref="CodeEditor"/>s — a VS-Code-style editor area. Each open document is a closable tab
/// (click the ✕ on the active/hovered tab), and a "+" button at the end of the bar opens a new document. Built on a
/// top-docked <see cref="TabPanel"/>; each editor is wrapped in its own frame so it scrolls independently. Switch
/// tabs with Alt+←/→ or by clicking a tab.
/// </summary>
public class MultiTabCodeEditor : CompositeControl
{
    #region Constructors
    public MultiTabCodeEditor(Language defaultLanguage = Language.None)
    {
        _defaultLanguage = defaultLanguage;
        _panel = new TabPanel(TabBarDock.Top) { ClosableTabs = true, ShowAddButton = true };
        _panel.NewTabRequested += () => NewDocument();
        _panel.TabCloseRequested += OnTabCloseRequested;   // route the ✕ through the cancelable DocumentClosing
        _panel.TabRemoved += OnTabRemoved;
        _panel.SelectionChanged += _ => ActiveDocumentChanged?.Invoke(ActiveEditor);
        SetContent(_panel);
    }
    #endregion

    #region Events
    /// <summary>Raised after a document is opened (its editor + tab exist and it is selected).</summary>
    public event Action<CodeEditor>? DocumentOpened;

    /// <summary>Raised before a document closes (via ✕ or <see cref="CloseDocument"/>). Set
    /// <see cref="DocumentClosingEventArgs.Cancel"/> to keep it open — e.g. after prompting about unsaved changes.</summary>
    public event EventHandler<DocumentClosingEventArgs>? DocumentClosing;

    /// <summary>Raised after a document's tab has been removed.</summary>
    public event Action<CodeEditor>? DocumentClosed;

    /// <summary>Raised after the active document changes (its editor, or <see langword="null"/> when none remain).</summary>
    public event Action<CodeEditor?>? ActiveDocumentChanged;
    #endregion

    #region Properties
    /// <summary>The underlying tab panel (for styling or advanced tab operations).</summary>
    public TabPanel Tabs => _panel;

    /// <summary>The selected document's editor, or <see langword="null"/> when none are open.</summary>
    public CodeEditor? ActiveEditor => _panel.ActiveTab?.Content as CodeEditor;

    /// <summary>The selected document's name (tab label), or <see langword="null"/> when none are open.</summary>
    public string? ActiveDocumentName => _panel.ActiveTabName;

    /// <summary>All open editors, in tab order.</summary>
    public IReadOnlyList<CodeEditor> Editors => _panel.Tabs.Select(t => (CodeEditor)t.Content).ToList();

    /// <summary>The number of open documents.</summary>
    public int DocumentCount => _panel.TabCount;
    #endregion

    #region Methods
    /// <summary>Opens a document in a new closable tab and selects it. Returns the created editor.</summary>
    public CodeEditor OpenDocument(string name, string text = "", Language? language = null)
    {
        CodeEditor editor = null!;
        UI.Invoke(() =>
        {
            editor = new CodeEditor(language ?? _defaultLanguage) { Text = text };
            editor.WithFrame(borderStyle: BorderStyle.None);   // each editor gets its own scroll viewport
            _panel.SelectTab(_panel.AddTab(name, editor));
        });
        DocumentOpened?.Invoke(editor);
        return editor;
    }

    /// <summary>Opens a new empty document with a generated "untitled-N" name.</summary>
    public CodeEditor NewDocument() => OpenDocument($"untitled-{++_untitled}", "", _defaultLanguage);

    /// <summary>Closes the given document (raising <see cref="DocumentClosing"/> first, which may cancel it).</summary>
    public void CloseDocument(CodeEditor editor)
    {
        if (TabOf(editor) is not { } tab) return;
        if (RaiseClosing(editor)) return;   // a handler canceled
        _panel.RemoveTab(tab);
    }

    /// <summary>Closes the active document (if any), honoring <see cref="DocumentClosing"/>.</summary>
    public void CloseActiveDocument() { if (ActiveEditor is { } e) CloseDocument(e); }

    /// <summary>Marks a document dirty or clean by toggling a leading "● " marker on its tab label.</summary>
    public void SetDirty(CodeEditor editor, bool dirty)
    {
        if (TabOf(editor) is not { } tab) return;
        var marked = tab.Name.StartsWith(DirtyMark, StringComparison.Ordinal);
        if (dirty && !marked) tab.Name = DirtyMark + tab.Name;
        else if (!dirty && marked) tab.Name = tab.Name[DirtyMark.Length..];
    }

    // The panel's ✕ asks to close: route it through the cancelable DocumentClosing so an owner can veto.
    private void OnTabCloseRequested(object? sender, TabCloseEventArgs e)
    {
        if (e.Tab.Content is CodeEditor editor && RaiseClosing(editor)) e.Cancel = true;
    }

    private void OnTabRemoved(TabItem tab)
    {
        if (tab.Content is CodeEditor editor) DocumentClosed?.Invoke(editor);
    }

    // Raises DocumentClosing; returns true when a handler canceled the close.
    private bool RaiseClosing(CodeEditor editor)
    {
        var args = new DocumentClosingEventArgs(editor);
        DocumentClosing?.Invoke(this, args);
        return args.Cancel;
    }

    private TabItem? TabOf(CodeEditor editor)
    {
        foreach (var t in _panel.Tabs) if (ReferenceEquals(t.Content, editor)) return t;
        return null;
    }

    protected internal override HelpInfo? GetHelpInfo() => new HelpInfo("Editors", "Editor Group",
        "A tabbed group of code editors.")
        .WithKey("Alt+← / Alt+→", "Switch tab")
        .WithKey("Click ✕", "Close tab")
        .WithKey("Click +", "New tab");
    #endregion

    #region Fields
    private readonly TabPanel _panel;
    private readonly Language _defaultLanguage;
    private int _untitled;
    private const string DirtyMark = "● ";
    #endregion
}

/// <summary>Arguments for <see cref="MultiTabCodeEditor.DocumentClosing"/>. Set <see cref="Cancel"/> to keep the
/// document open (e.g. after confirming unsaved changes).</summary>
public sealed class DocumentClosingEventArgs : EventArgs
{
    internal DocumentClosingEventArgs(CodeEditor editor) => Editor = editor;

    /// <summary>The document's editor.</summary>
    public CodeEditor Editor { get; }

    /// <summary>Set to <see langword="true"/> to cancel the close and keep the document open.</summary>
    public bool Cancel { get; set; }
}
