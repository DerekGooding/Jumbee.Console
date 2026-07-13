namespace Jumbee.Console.DocumentViewers;

using System;
using System.Collections.Generic;

using Mermaider;

using Jumbee.Console.DocumentViewers.Mermaid;

/// <summary>
/// Parses Mermaid source and rasterizes it to a <see cref="CellCanvas"/>, routing by diagram type. Shared by
/// <see cref="MermaidViewer"/> (which blits the canvas to its own buffer) and <see cref="AsciiDocRenderer"/> (which
/// wraps it as an <see cref="Spectre.Console.Rendering.IRenderable"/> for an embedded <c>[source,mermaid]</c> block).
/// </summary>
internal static class MermaidCanvas
{
    // Routes by diagram type: class/ER/sequence use vendored parsers (Mermaider's are internal) + the public layout
    // (sequence lays out in cell space directly); flowchart/state go through the public MermaidRenderer.Parse.
    public static CellCanvas Build(string text, MermaidStyles styles)
    {
        var lines = PreprocessLines(text);
        if (lines.Length > 0 && lines[0].StartsWith("classDiagram", StringComparison.Ordinal))
        {
            var diagram = ClassParser.Parse(lines);
            var positioned = MermaidRenderer.LayoutProvider.LayoutClass(diagram);
            return new MermaidClassRenderer(styles).Render(positioned);
        }
        if (lines.Length > 0 && lines[0].StartsWith("erDiagram", StringComparison.Ordinal))
        {
            var diagram = ErParser.Parse(lines);
            var positioned = MermaidRenderer.LayoutProvider.LayoutEr(diagram);
            return new MermaidErRenderer(styles).Render(positioned);
        }
        if (lines.Length > 0 && lines[0].StartsWith("sequenceDiagram", StringComparison.Ordinal))
        {
            // Sequence has no public Mermaider layout, so we lay the parsed model out ourselves in cell space.
            var diagram = SequenceParser.Parse(lines);
            return new MermaidSequenceRenderer(styles).Render(diagram);
        }

        var graph = MermaidRenderer.Parse(text);
        var flow = MermaidRenderer.LayoutProvider.LayoutFlowchart(graph);
        return new MermaidFlowchartRenderer(styles).Render(flow);
    }

    // Trim, drop blank lines and %% comments — the line shape the vendored parsers expect (header at index 0).
    private static string[] PreprocessLines(string text)
    {
        var raw = text.Split('\n');
        var list = new List<string>(raw.Length);
        foreach (var r in raw)
        {
            var t = r.Trim();
            if (t.Length > 0 && !t.StartsWith("%%", StringComparison.Ordinal)) list.Add(t);
        }
        return list.ToArray();
    }
}
