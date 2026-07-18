namespace Jumbee.Console.Documents;

using System.Collections.Generic;

using ColorCode;
using ColorCode.Common;

/// <summary>
/// A ColorCode <see cref="ILanguage"/> grammar for AsciiDoc source, for syntax-highlighting an AsciiDoc document in a
/// <see cref="CodeEditor"/> (<c>new CodeEditor(AsciiDocLanguage.Instance)</c>).
/// </summary>
/// <remarks>
/// AsciiDoc's parser (AdocNet) is
/// structure-based rather than regex-based, so — as with the Ace editor's AsciiDoc mode — the highlighter uses its own
/// token regexes (adapted from espadrine's Ace-mode gist), mapping AsciiDoc constructs onto the Markdown-family scopes
/// that the default syntax theme colours.
/// <para>
/// Rules are applied in list order and the first to match a position wins, so line-level constructs (comments,
/// headings, block titles/attributes, admonitions, attribute entries, list markers) come before the inline formatting
/// (links, monospace, bold, italic) — a heading line claims its whole row before an inline rule can recolour part of it.
/// </para>
/// </remarks>
public sealed class AsciiDocLanguage : ILanguage
{
    #region Singleton
    /// <summary>The shared grammar instance (ColorCode caches the compiled grammar by <see cref="Id"/>).</summary>
    public static readonly AsciiDocLanguage Instance = new();

    private AsciiDocLanguage() { }
    #endregion

    #region ILanguage
    /// <summary>The ColorCode language id (<c>"asciidoc"</c>).</summary>
    public string Id => "asciidoc";
    /// <summary>The display name.</summary>
    public string Name => "AsciiDoc";
    /// <summary>The CSS class name used for HTML output.</summary>
    public string CssClassName => "asciidoc";
    /// <summary>First-line detection pattern (unused; <see langword="null"/>).</summary>
    public string FirstLinePattern => null!;
    /// <summary>Returns <see langword="true"/> when <paramref name="lang"/> is a known alias (<c>asciidoc</c>/<c>adoc</c>/<c>asc</c>).</summary>
    public bool HasAlias(string lang) => lang is "asciidoc" or "adoc" or "asc";
    /// <inheritdoc/>
    public override string ToString() => Name;

    /// <summary>The ordered highlighting rules (the first to match a position wins).</summary>
    public IList<LanguageRule> Rules => new List<LanguageRule>
    {
        // // line comment (also colours a //// comment-block fence line; content between fences is not tracked).
        Rule(@"(^//.*)$", ScopeName.Comment),

        // Section headings: "= Title" through "====== Title" (needs whitespace + text so it doesn't grab a ==== fence).
        Rule(@"(^={1,6}\s+\S.*)$", ScopeName.MarkdownHeader),

        // Block title ".Title" (a leading dot followed by a non-dot, non-space).
        Rule(@"(^\.[^.\s].*)$", ScopeName.Attribute),

        // Block attribute list: [source,java] / [NOTE] / [cols="1,1"].
        Rule(@"(^\[.*\])\s*$", ScopeName.Attribute),

        // Admonition lead-ins.
        Rule(@"(^(?:NOTE|TIP|IMPORTANT|WARNING|CAUTION):)", ScopeName.ControlKeyword),

        // Attribute entry ":name:" / ":!name:".
        Rule(@"(^:[\w!-][\w-]*:)", ScopeName.Keyword),

        // List markers: * - . (1–5) bullets, or "1." / "a." ordered, at line start; and <1> callouts.
        Rule(@"(^\s*(?:\*{1,5}|-|\.{1,5}|\d+\.|[a-zA-Z]\.)\s)", ScopeName.MarkdownListItem),
        Rule(@"(^\s*<\d+>\s)", ScopeName.MarkdownListItem),

        // Cross-reference <<ref>> and anchor [[id]].
        Rule(@"(<<[^>\n]+>>)", ScopeName.Keyword),
        Rule(@"(\[\[[^\]\n]+\]\])", ScopeName.Keyword),

        // Links / URLs (bare, or the target of a link:/image: macro; any following [text] stays plain).
        Rule(@"((?:https?|ftp|file|mailto|link|image|irc):[^\s\[\]]+)", ScopeName.String),

        // Attribute reference {name}.
        Rule(@"(\{[\w-]+\})", ScopeName.Attribute),

        // Inline monospace `code`.
        Rule(@"(`[^`\n]+`)", ScopeName.MarkdownCode),

        // Inline bold *text* / **text** (a non-space after the marker keeps a "* " list bullet out of this).
        Rule(@"(\*{1,2}[^\s*][^*\n]*?\*{1,2})", ScopeName.MarkdownBold),

        // Inline italic _text_ / __text__.
        Rule(@"(_{1,2}[^\s_][^_\n]*?_{1,2})", ScopeName.MarkdownEmph),
    };
    #endregion

    #region Helpers
    private static LanguageRule Rule(string pattern, string scope) =>
        new(pattern, new Dictionary<int, string> { { 1, scope } });
    #endregion
}
