namespace Jumbee.Console.AgentHarnessDemo;

using System;
using System.Threading.Tasks;

using Jumbee.Console;

/// <summary>
/// Fakes an agent turn. On submit it pushes the user's prompt, spins the prompt's busy indicator, then plays a
/// scripted sequence — streaming assistant prose, tool-call chips that flip from running to done, and task-list
/// steps advancing ○ → ● → ✓ — with small delays so it reads like a live session. No real model is involved.
/// </summary>
internal sealed class AgentSimulator(TranscriptView transcript, TaskListView tasks, MarkdownViewer doc, ChatPrompt prompt)
{
    #region Methods
    /// <summary>Plays one scripted turn for <paramref name="userText"/>. Ignored while a turn is already running or
    /// the text is blank.</summary>
    public void Run(string userText)
    {
        if (string.IsNullOrWhiteSpace(userText) || _busy) return;
        _ = Play(userText.Trim());
    }

    private async Task Play(string userText)
    {
        _busy = true;
        try
        {
            await OnUi(() =>
            {
                transcript.AddUser(userText);
                prompt.Text = "";
                prompt.Busy = true;
            });
            await Task.Delay(450);

            var reply = await OnUi(() => transcript.AddAssistant());
            await Stream(reply,
                $"On it — I'll finish the integration tests, add [{Coral}]nextCursor[/] to the OpenAPI schema, " +
                $"regenerate the client types, then open the PR.");

            // Walk the seeded checklist forward, one tool call per beat (AdvanceStep completes the active step and
            // promotes the next pending one).
            await Chip("◆", "Edited orders.integration.test.ts", Palette.Green, "+90 -20", 900);
            await OnUi(() => tasks.AdvanceStep());
            await Chip("▸", "Ran a command — dotnet test", Palette.Yellow, null, 900);
            var green = await OnUi(() => transcript.AddAssistant());
            await Stream(green, $"[{Green}]All 24 tests green.[/]");

            await Chip("◆", "Edited openapi.yaml", Palette.Green, "+14 -0", 700);
            await OnUi(() => tasks.AdvanceStep());
            await Chip("▸", "Ran a command — npm run gen:client", Palette.Yellow, null, 800);
            await OnUi(() => tasks.AdvanceStep());
            await Chip("▸", "Ran a command — gh pr create", Palette.Yellow, null, 900);
            await OnUi(() => tasks.AdvanceStep());

            var summary = await OnUi(() => transcript.AddAssistant());
            await Stream(summary,
                $"Done — cursor pagination is [{Green}]ready for review[/]. PR [{Coral}]#482[/] is open: opaque cursor, " +
                $"a `(created_at, id)` tiebreaker, a `nextCursor`/`hasMore` envelope, and the OpenAPI schema + client " +
                $"types updated. Left the admin-export offset fallback out, per the design doc.");

            await OnUi(() => doc.Markdown = DocWithUpdate);
        }
        catch { /* a demo swallows script errors rather than crashing the UI thread */ }
        finally
        {
            await OnUi(() => prompt.Busy = false);
            _busy = false;
        }
    }

    // Streams `markup` into an assistant block word-by-word so the transcript fills in live under the busy spinner.
    private async Task Stream(AssistantBlock block, string markup)
    {
        var words = markup.Split(' ');
        for (var i = 0; i < words.Length; i++)
        {
            var word = words[i];
            await OnUi(() =>
            {
                block.Markdown += (block.Markdown.Length == 0 ? "" : " ") + word;
                transcript.Refresh();
            });
            await Task.Delay(28);
        }
    }

    // Adds a running tool chip, waits, then flips it to done.
    private async Task Chip(string glyph, string label, Color accent, string? detail, int ms)
    {
        var chip = await OnUi(() => transcript.AddTool(glyph, label, accent, detail));
        await Task.Delay(ms);
        await OnUi(() => { chip.Status = ToolStatus.Done; transcript.Refresh(); });
    }

    // Runs `action` on the UI thread and completes when it has run.
    private static Task OnUi(Action action)
    {
        var tcs = new TaskCompletionSource();
        UI.Post(() => { try { action(); tcs.SetResult(); } catch (Exception e) { tcs.SetException(e); } });
        return tcs.Task;
    }

    private static Task<T> OnUi<T>(Func<T> func)
    {
        var tcs = new TaskCompletionSource<T>();
        UI.Post(() => { try { tcs.SetResult(func()); } catch (Exception e) { tcs.SetException(e); } });
        return tcs.Task;
    }

    private static string Coral => ((Style)Palette.Coral).ToMarkup();
    private static string Green => ((Style)Palette.Green).ToMarkup();
    #endregion

    #region Fields
    private const string DocWithUpdate =
        "# Pagination design\n\n" +
        "## /orders endpoint\n\n" +
        "**Status:** shipped · PR #482 open for review\n\n" +
        "## Decision\n\n" +
        "**Cursor (keyset) pagination** ordered by `(created_at, id)`:\n\n" +
        "- `GET /orders?limit=50&cursor=<opaque>` — base64 of `{ createdAt, id }`.\n" +
        "- Seeks `WHERE (created_at, id) < (@ts, @id) ORDER BY created_at DESC, id DESC LIMIT @limit`.\n" +
        "- Envelope: `{ data, nextCursor, hasMore }`.\n\n" +
        "## Shipped in PR #482\n\n" +
        "- Opaque cursor + `(created_at, id)` tiebreaker (fixes the same-timestamp paging bug).\n" +
        "- `nextCursor` / `hasMore` response envelope.\n" +
        "- OpenAPI schema updated and client types regenerated.\n" +
        "- **All 24 integration tests green.**\n\n" +
        "## Open questions\n\n" +
        "1. ~~Do we need an offset fallback for the admin export?~~ **Resolved: no — use a streaming export.**\n" +
        "2. Cap `limit` at 200? *(yes for now)*\n";

    private bool _busy;
    #endregion
}
