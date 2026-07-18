namespace Jumbee.Console.Documents;

using System.Collections.Generic;

using ColorCode;
using ColorCode.Common;

/// <summary>
/// A ColorCode <see cref="ILanguage"/> grammar for Mermaid diagram source, for syntax-highlighting a Mermaid document
/// in a <see cref="CodeEditor"/> (<c>new CodeEditor(MermaidLanguage.Instance)</c>).
/// </summary>
/// <remarks>
/// The token patterns are lifted from
/// the vendored Mermaider parsers (<c>ext/Mermaider/Parsing/*.cs</c>): the diagram-type and structural keyword lists,
/// the direction set, and the flowchart/sequence/class/ER arrow and relationship alternations. It classifies tokens
/// (it does not validate structure), so it highlights every diagram type uniformly.
/// <para>
/// Rules are applied in list order and the first to match a position wins, so the order is: comments, then quoted
/// strings and bracket labels (which must claim their text before keyword rules can colour a keyword sitting inside a
/// label), then arrows, then keywords, directions and numbers.
/// </para>
/// </remarks>
public sealed class MermaidLanguage : ILanguage
{
    #region Singleton
    /// <summary>The shared grammar instance (ColorCode caches the compiled grammar by <see cref="Id"/>).</summary>
    public static readonly MermaidLanguage Instance = new();

    private MermaidLanguage() { }
    #endregion

    #region ILanguage
    public string Id => "mermaid";
    public string Name => "Mermaid";
    public string CssClassName => "mermaid";
    public string FirstLinePattern => null!;
    public bool HasAlias(string lang) => lang is "mermaid" or "mmd";
    public override string ToString() => Name;

    public IList<LanguageRule> Rules => new List<LanguageRule>
    {
        // %% line/trailing comment (incl. the %%{init:...}%% directive). Mermaid's only comment form.
        Rule(@"(%%.*)$", ScopeName.Comment),

        // Quoted string, and [bracket] labels (node text / entity aliases). '[' is never part of an arrow, so a
        // square-bracket label is safe to claim here; '(' and '{' are reused by arrows/cardinality so are left alone.
        Rule(@"(""[^""]*"")", ScopeName.String),
        Rule(@"(\[[^\]]*\])", ScopeName.String),

        // Arrows / edges / relationships — the alternations from the flowchart/sequence/class/ER parsers, ordered
        // longest-first so e.g. "-->" beats "--". The ER-cardinality form (|,o,{,} around -- or ..) is last.
        Rule(@"(<\|--|<\|\.\.|--\|>|\.\.\|>|\*--|--\*|--o|o--|<<-->>|<<->>|-->>|--x|--\)|-\.->|~{3,}|={2,}>|-{2,}>|={3,}|-{3,}|->>|<-->|<->|-->|<--|<\.\.|\.\.>|-x|-\)|->|==|--|\.\.|[|o}{]{1,2}(?:--|\.\.)[|o}{]{1,2})",
             ScopeName.ControlKeyword),

        // Diagram-type headers (DiagramDetector / FlowchartParser). Longest-first so stateDiagram-v2 beats stateDiagram.
        Rule(@"\b(sequenceDiagram|classDiagram|erDiagram|stateDiagram-v2|stateDiagram|flowchart|graph|gitGraph|mindmap|timeline|quadrantChart|requirementDiagram|journey|gantt|pie|xychart-beta|sankey-beta|block-beta|radar-beta|treemap-beta|venn-beta|C4Context)\b",
             ScopeName.Keyword),

        // Structural keywords across the diagram types (subgraph/participant/loop/alt/class/state/note/...).
        Rule(@"\b(subgraph|end|direction|participant|actor|note|Note|loop|alt|else|opt|par|and|critical|break|rect|activate|deactivate|autonumber|box|create|destroy|classDef|class|linkStyle|style|namespace|state|section|title|accTitle|accDescr|commit|branch|checkout|switch|merge|cherry-pick|callback|click|href|call|link|over|as|of|left|right|showData)\b",
             ScopeName.Keyword),

        // Flow directions.
        Rule(@"\b(TD|TB|LR|RL|BT)\b", ScopeName.ClassName),

        // Numbers (autonumber start/step, pie/quadrant/radar values).
        Rule(@"\b(\d+(?:\.\d+)?)\b", ScopeName.Number),
    };
    #endregion

    #region Helpers
    private static LanguageRule Rule(string pattern, string scope) =>
        new(pattern, new Dictionary<int, string> { { 1, scope } });
    #endregion
}
