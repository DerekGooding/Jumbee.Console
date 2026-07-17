namespace Jumbee.Console.AgentHarnessDemo;

using Jumbee.Console;

// Shared data the custom controls render and the AgentSimulator drives. Kept as plain mutable records/classes: the
// simulator mutates a returned handle (stream text into an AssistantBlock, flip a ToolBlock to Done) and then calls
// the owning control's Refresh(), rather than the model notifying — no eventing, no threading concerns off the UI thread.

/// <summary>A row in the left "Recents" session list.</summary>
internal sealed record SessionItem(string Title, bool Warn = false, bool Active = false);

// ── Transcript blocks (centre pane) ──────────────────────────────────────────────────────────────────────────────

/// <summary>Base for an ordered entry in the <see cref="TranscriptView"/>.</summary>
internal abstract class ChatBlock;

/// <summary>A user's prompt — drawn as a right-aligned rounded bubble.</summary>
internal sealed class UserBlock(string text) : ChatBlock
{
    public string Text { get; set; } = text;
}

/// <summary>A run of assistant prose, rendered as Markdown. <see cref="Markdown"/> is appended to while streaming.</summary>
internal sealed class AssistantBlock(string markdown = "") : ChatBlock
{
    public string Markdown { get; set; } = markdown;
}

internal enum ToolStatus { Running, Done, Error }

/// <summary>An inline tool-call chip — a glyph, a label, an optional detail suffix (e.g. <c>+29 -0</c>), and a status.
/// Mirrors the "Read 6 files ›" / "Edited RESEARCH-JOURNAL.md +29 -0" rows in the harness.</summary>
internal sealed class ToolBlock(string glyph, string label, Color accent, string? detail = null) : ChatBlock
{
    public string Glyph { get; set; } = glyph;
    public string Label { get; set; } = label;
    public string? Detail { get; set; } = detail;
    public Color Accent { get; set; } = accent;
    public ToolStatus Status { get; set; } = ToolStatus.Running;
}

// ── Task list steps (top-right pane) ─────────────────────────────────────────────────────────────────────────────

internal enum StepStatus { Pending, Active, Done, Failed }

/// <summary>One step in the <see cref="TaskListView"/> checklist. Sub-steps use <see cref="Indent"/> = 1
/// (e.g. a "Read 6 files ›" roll-up under an active step).</summary>
internal sealed class AgentStep(string text, int indent = 0)
{
    public string Text { get; set; } = text;
    public int Indent { get; init; } = indent;
    public StepStatus Status { get; set; } = StepStatus.Pending;
}
