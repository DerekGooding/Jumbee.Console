
using ColorCode;
using ColorCode.Common;

namespace Jumbee.Console.Documents;
/// <summary>
/// A ColorCode <see cref="ILanguage"/> that highlights Markdown <em>and</em> the contents of embedded
/// <c>```mermaid</c> fenced blocks (using the <see cref="MermaidLanguage"/> grammar) — for editing Markdown that
/// contains mermaid diagrams in a <see cref="CodeEditor"/>.
/// </summary>
/// <remarks>
/// It reuses the built-in ColorCode Markdown rules and
/// prepends a <c>```mermaid</c>-fence rule whose inner content is delegated to the nested "mermaid" grammar via
/// ColorCode's language-embedding mechanism (a capture scope prefixed with <see cref="ScopeName.LanguagePrefix"/>).
/// </remarks>
public sealed class MarkdownWithMermaidLanguage : ILanguage
{
    #region Singleton

    /// <summary>The shared grammar instance (ColorCode caches the compiled grammar by <see cref="Id"/>).</summary>
    public static readonly MarkdownWithMermaidLanguage Instance = new();

    private MarkdownWithMermaidLanguage()
    {
        // The nested "&mermaid" reference in the fence rule is resolved at parse time via ColorCode's global language
        // repository (the segment formatter's default parser binds to it), so register the Mermaid grammar there once.
        if (Languages.FindById(MermaidLanguage.Instance.Id) is null)
            Languages.Load(MermaidLanguage.Instance);
    }

    #endregion Singleton

    #region ILanguage

    /// <summary>The ColorCode language id (<c>"markdown-mermaid"</c>).</summary>
    public string Id => "markdown-mermaid";

    /// <summary>The display name.</summary>
    public string Name => "Markdown (with Mermaid)";

    /// <summary>The CSS class name used for HTML output.</summary>
    public string CssClassName => "markdown-mermaid";

    /// <summary>First-line detection pattern (unused; <see langword="null"/>).</summary>
    public string FirstLinePattern => null!;

    /// <summary>Always <see langword="false"/> — this grammar has no aliases.</summary>
    public bool HasAlias(string lang) => false;

    /// <inheritdoc/>
    public override string ToString() => Name;

    /// <summary>The highlighting rules: the <c>```mermaid</c>-fence rule followed by the standard Markdown rules.</summary>
    public IList<LanguageRule> Rules
    {
        get
        {
            var rules = new List<LanguageRule>
            {
                // A ```mermaid fence: opener line, inner content (recursively highlighted by the nested Mermaid
                // grammar), then the closing fence. Placed first so it wins over the generic Markdown code-block rule
                // (which would otherwise colour the whole block as plain MarkdownCode). Multi-line content is matched
                // with an explicit (.|\r?\n) since ColorCode compiles each rule without the singleline (?s) flag.
                new LanguageRule(
                    @"(^```+[ \t]*mermaid[ \t]*\r?\n)((?:.|\r?\n)*?)(^```+[ \t]*\r?$)",
                    new Dictionary<int, string>
                    {
                        { 1, ScopeName.XmlDocTag },                                     // ```mermaid opener
                        { 2, ScopeName.LanguagePrefix + MermaidLanguage.Instance.Id },  // inner -> "&mermaid"
                        { 3, ScopeName.XmlDocTag },                                     // closing ```
                    }),
            };

            // Then all the standard Markdown rules (headings, bold/italic, lists, links, the generic code block, …).
            rules.AddRange(new ColorCode.Compilation.Languages.Markdown().Rules);
            return rules;
        }
    }

    #endregion ILanguage
}