namespace Jumbee.Console.Examples;

using System;
using System.IO;
using System.Reflection;

/// <summary>
/// Reads example source that was embedded into the assembly (see the <c>EmbeddedResource</c> glob in the csproj), so
/// the right-pane viewer shows exactly what compiled — and maps a file extension to a syntax-highlighting
/// <see cref="Language"/>.
/// </summary>
public static class SourceLoader
{
    private static readonly Assembly Asm = typeof(SourceLoader).Assembly;

    /// <summary>The text of the embedded file whose resource name ends with <paramref name="fileName"/>, or a short
    /// placeholder when it isn't found.</summary>
    public static string Read(string fileName)
    {
        if (ResourceName(fileName) is not { } name) return $"// source not found: {fileName}";
        using var stream = Asm.GetManifestResourceStream(name);
        if (stream is null) return $"// source not found: {fileName}";
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    /// <summary>Whether an embedded source file matching <paramref name="fileName"/> exists (used by <c>--verify</c>).</summary>
    public static bool Exists(string fileName) => ResourceName(fileName) is not null;

    /// <summary>The syntax language for a file, by extension.</summary>
    public static Language LanguageFor(string fileName) => Path.GetExtension(fileName).ToLowerInvariant() switch
    {
        ".cs" => Language.CSharp,
        ".md" => Language.Markdown,
        ".json" => Language.Json,
        ".xml" => Language.Xml,
        ".yaml" or ".yml" => Language.Yaml,
        _ => Language.None,
    };

    // Resource names are "{RootNamespace}.{path.with.dots}.{file}", so match by the ".{fileName}" suffix — robust to
    // the namespace/folder prefix.
    private static string? ResourceName(string fileName) =>
        Array.Find(Asm.GetManifestResourceNames(), n => n.EndsWith("." + fileName, StringComparison.Ordinal));
}
