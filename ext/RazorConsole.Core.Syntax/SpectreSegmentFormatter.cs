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

    protected override void Write(string parsedSourceCode, IList<Scope> scopes)
    {
        if (parsedSourceCode.Length == 0)
        {
            return;
        }

        AppendSegment(parsedSourceCode.AsSpan(), scopes, _currentTheme.DefaultStyle);
    }

    private void AppendSegment(ReadOnlySpan<char> content, IList<Scope> scopes, SpectreStyle parentStyle)
    {
        if (scopes is null || scopes.Count == 0)
        {
            AppendStyled(content, parentStyle);
            return;
        }

        var ordered = scopes.OrderBy(static scope => scope.Index).ToArray();
        var position = 0;
        foreach (var scope in ordered)
        {
            var relativeIndex = Math.Clamp(scope.Index, 0, content.Length);
            if (relativeIndex > position)
            {
                AppendStyled(content.Slice(position, relativeIndex - position), parentStyle);
            }

            var scopeLength = Math.Clamp(scope.Length, 0, content.Length - relativeIndex);
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

        if (position < content.Length)
        {
            AppendStyled(content[position..], parentStyle);
        }
    }

    private void AppendStyled(ReadOnlySpan<char> content, SpectreStyle style)
    {
        if (content.Length == 0)
        {
            return;
        }

        // Tab expansion only allocates when enabled; the editor uses TabWidth == 0 (no expansion). Newlines are
        // left embedded — the buffer's segment writer splits on '\n'.
        var text = _options.TabWidth > 0 ? ExpandTabs(content) : content.ToString();
        _segments.Add(new Segment(text, style));
    }

    private string ExpandTabs(ReadOnlySpan<char> content)
    {
        var tabReplacement = new string(' ', _options.TabWidth);
        return content.ToString().Replace("\r\n", "\n", StringComparison.Ordinal)
                                 .Replace("\r", "\n", StringComparison.Ordinal)
                                 .Replace("\t", tabReplacement, StringComparison.Ordinal);
    }
}
