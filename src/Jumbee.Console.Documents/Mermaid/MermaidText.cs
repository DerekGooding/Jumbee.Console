namespace Jumbee.Console.Documents.Mermaid;

/// <summary>
/// Minimal replacement for Mermaider's internal <c>MultilineUtils</c>, used by the vendored <see cref="ClassParser"/>.
/// Only the subset the parser needs for console rendering: strip surrounding quotes and turn <c>&lt;br&gt;</c> into
/// newlines (SVG-specific markdown/tag normalization is irrelevant to a text console).
/// </summary>
internal static class MultilineUtils
{
    public static string NormalizeBrTags(string label)
    {
        var s = label is ['"', .., '"'] ? label[1..^1] : label;
        return s.Replace("<br/>", "\n").Replace("<br />", "\n").Replace("<br>", "\n");
    }
}