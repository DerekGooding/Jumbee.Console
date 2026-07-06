// Copyright (c) RazorConsole. All rights reserved.

using System.Text;
using ColorCode;
using ColorCode.Parsing;
using ColorCode.Styling;
using Spectre.Console;
using Spectre.Console.Rendering;
using SpectreStyle = Spectre.Console.Style;

namespace RazorConsole.Core.Rendering.Syntax;

/// <summary>
/// A syntax highlighter that emits Spectre <see cref="Segment"/>s directly from ColorCode scopes — no markup
/// string intermediate, so no <see cref="Markup.Escape"/> and no Spectre markup re-parse. This is the fast
/// counterpart to <see cref="SpectreMarkupFormatter"/>: the caller writes the returned segments straight into
/// its render buffer (e.g. <c>AnsiConsoleBuffer.Write(IReadOnlyList&lt;Segment&gt;)</c>). Newlines stay embedded
/// in segment text (the buffer splits on <c>'\n'</c>).
/// </summary>
public sealed class SpectreSegmentFormatter : CodeColorizerBase
{
    private readonly List<Segment> _segments = new();
    private SyntaxTheme _currentTheme = null!;
    private SyntaxOptions _options = null!;

    public SpectreSegmentFormatter()
        : base(StyleDictionary.DefaultLight, languageParser: null)
    {
    }

    /// <summary>
    /// Highlights <paramref name="sourceCode"/> and returns the styled segments. The list is reused between calls,
    /// so consume it before calling <see cref="Format"/> again.
    /// </summary>
    public IReadOnlyList<Segment> Format(string sourceCode, ILanguage language, SyntaxTheme theme, SyntaxOptions options)
    {
        ArgumentNullException.ThrowIfNull(sourceCode);
        ArgumentNullException.ThrowIfNull(language);
        ArgumentNullException.ThrowIfNull(theme);
        ArgumentNullException.ThrowIfNull(options);

        _segments.Clear();
        _currentTheme = theme;
        _options = options;

        languageParser.Parse(sourceCode, language, Write);

        return _segments;
    }

    protected override void Write(ReadOnlyMemory<char> parsedSourceCode, IList<Scope> scopes)
    {
        if (parsedSourceCode.Length == 0)
        {
            return;
        }

        // parsedSourceCode is already a zero-copy slice of the original source string (no per-fragment Substring);
        // every token becomes a further slice of it.
        AppendSegment(parsedSourceCode, scopes, _currentTheme.DefaultStyle);
    }

    private void AppendSegment(ReadOnlyMemory<char> content, IList<Scope> scopes, SpectreStyle parentStyle)
    {
        if (scopes is null || scopes.Count == 0)
        {
            AppendStyled(content, parentStyle);
            return;
        }

        // ColorCode usually emits sibling scopes already ordered by Index; only pay the OrderBy + array allocation
        // when they aren't. The sorted check is an alloc-free O(n) scan.
        var ordered = IsSortedByIndex(scopes) ? scopes : scopes.OrderBy(static scope => scope.Index).ToArray();
        var length = content.Length;
        var position = 0;
        for (var i = 0; i < ordered.Count; i++)
        {
            var scope = ordered[i];
            var relativeIndex = Math.Clamp(scope.Index, 0, length);
            if (relativeIndex > position)
            {
                AppendStyled(content.Slice(position, relativeIndex - position), parentStyle);
            }

            var scopeLength = Math.Clamp(scope.Length, 0, length - relativeIndex);
            if (scopeLength <= 0)
            {
                continue;
            }

            var segment = content.Slice(relativeIndex, scopeLength);
            var scopeStyle = _currentTheme.GetStyle(scope.Name);
            if (scope.Children.Count > 0)
            {
                AppendSegment(segment, scope.Children, scopeStyle);
            }
            else
            {
                AppendStyled(segment, scopeStyle);
            }

            position = relativeIndex + scopeLength;
        }

        if (position < length)
        {
            AppendStyled(content.Slice(position), parentStyle);
        }
    }

    private void AppendStyled(ReadOnlyMemory<char> content, SpectreStyle style)
    {
        if (content.Length == 0)
        {
            return;
        }

        // Both paths use the zero-copy, non-normalizing slice ctor so tab-on and tab-off render identically.
        // TabWidth == 0 (the editor): the source chunk slice as-is — no per-token string. TabWidth > 0: expand
        // tabs into one new string (only when tabs are actually present). Newlines stay embedded; the buffer's
        // segment writer handles '\n'/'\r'.
        var text = _options.TabWidth > 0 ? ExpandTabs(content) : content;
        _segments.Add(new Segment(text, style, lineBreak: false, control: false));
    }

    private static bool IsSortedByIndex(IList<Scope> scopes)
    {
        for (var i = 1; i < scopes.Count; i++)
        {
            if (scopes[i].Index < scopes[i - 1].Index)
            {
                return false;
            }
        }

        return true;
    }

    // Expands each '\t' to TabWidth spaces. Zero-copy (returns the input) when there are no tabs; otherwise a
    // single string allocation via string.Create — one pass, no per-Replace intermediates and no space-run string.
    private ReadOnlyMemory<char> ExpandTabs(ReadOnlyMemory<char> content)
    {
        var tabs = content.Span.Count('\t');
        if (tabs == 0)
        {
            return content;
        }

        var width = _options.TabWidth;
        var length = content.Length + (tabs * (width - 1));
        return string.Create(length, (content, width), static (dest, state) =>
        {
            var (source, tabWidth) = state;
            var src = source.Span;
            var j = 0;
            for (var i = 0; i < src.Length; i++)
            {
                var c = src[i];
                if (c == '\t')
                {
                    dest.Slice(j, tabWidth).Fill(' ');
                    j += tabWidth;
                }
                else
                {
                    dest[j++] = c;
                }
            }
        }).AsMemory();
    }
}
