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

            // Close out the step that was mid-flight in the seed, then open the new increment.
            await OnUi(() => tasks.AddStep("Green-lit increment 2 (ObfuscationAnalysis retarget)").Status = StepStatus.Done);
            var reply = await OnUi(() => transcript.AddAssistant());
            await Stream(reply,
                $"On it — retargeting [{Coral}]ObfuscationAnalysis[/] onto the CCI reader so the whole capability " +
                $"scan runs off one lock-free [{Coral}]CciAssembly[/] instance.");

            var scanStep = await OnUi(() => tasks.AddStep("Retarget ObfuscationAnalysis onto CciAssembly"));
            await OnUi(() => scanStep.Status = StepStatus.Active);
            await OnUi(() => tasks.Refresh());

            await Chip("◇", "Read 4 files", Palette.Blue, null, 700);
            await Chip("◆", "Edited ObfuscationAnalysis.cs", Palette.Green, "+64 -12", 900);
            await OnUi(() =>
            {
                scanStep.Status = StepStatus.Done;
                tasks.AddStep("Ran capability scan (55 IL, lock-free)").Status = StepStatus.Active;
                tasks.Refresh();
            });

            await Chip("▸", "Ran a command — dotnet test", Palette.Yellow, null, 1000);

            var summary = await OnUi(() => transcript.AddAssistant());
            await Stream(summary,
                $"Done. ObfuscationAnalysis now shares the CCI reader — [{Green}]55/55 IL tests green[/], no new " +
                $"allocations on the scan path. Journal updated; the SharpCompress [{Coral}]NU1902[/] call is still yours.");

            await OnUi(() =>
            {
                doc.Markdown = DocWithUpdate;
                tasks.Refresh();
            });
            await OnUi(MarkAllActiveDone);
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

    private void MarkAllActiveDone()
    {
        // The last active step completes at the end of the turn.
        tasks.AddStep("Committed increment 2").Status = StepStatus.Done;
        tasks.Refresh();
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
        "# Prior-art landscape\n\n" +
        "## supply-chain / capability analysis for package security\n\n" +
        "**Compiled:** 2026-07-16 · **Updated:** increment 2 landed.\n\n" +
        "## Executive summary\n\n" +
        "- CIL-level (.NET) capability analysis remains **unoccupied**; Silvergun is the first for the CLR.\n" +
        "- **ObfuscationAnalysis** now runs off the shared `CciAssembly` reader — lock-free, 55/55 IL tests green.\n\n" +
        "## Comparators\n\n" +
        "| Tool | Target | Level |\n" +
        "|------|--------|-------|\n" +
        "| Capslock | Go | source + call graph |\n" +
        "| JCapsLock | Java | bytecode |\n" +
        "| cargo-capslock | Rust | LLVM IR |\n" +
        "| **Silvergun** | **.NET** | **CIL** |\n\n" +
        "## Open questions\n\n" +
        "1. ~~Does the AppInspector `RulesEngine` consume cleanly as a library?~~ **Resolved: yes.**\n" +
        "2. Bump vs pin vs accept for the SharpCompress `NU1902` advisory. *(still open)*\n";

    private bool _busy;
    #endregion
}
