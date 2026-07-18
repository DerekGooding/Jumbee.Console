# Multithreading & the UI Thread

This document describes how Jumbee.Console handles threading: how the UI is updated and rendered, how
background threads safely mutate controls, and the rules control authors must follow.

## Mental model

Jumbee.Console uses a **single-threaded UI model with a dispatcher** (the WPF/Avalonia model):

- All UI state mutation and all rendering happen on **one dedicated UI thread**.
- There is **no shared UI lock**.
- Other threads do not touch UI state directly; they **marshal** work onto the UI thread via the dispatcher.
- To preserve ease-of-use ("update a control from anywhere"), marshaling is **automatic**: a control mutation
  called from a background thread is *posted* to the UI thread rather than throwing. (This is the deliberate
  difference from frameworks that *fail fast* on cross-thread access.)

There are exactly two OS threads involved at runtime:

1. **The UI thread** — owned by the `Dispatcher`. Runs the frame loop: drain work queue → dispatch input →
   render.
2. **The input reader thread** — blocks on the console reading keys and *posts* each key onto the UI thread.
   It never touches UI state itself.

Everything else (animation, layout, input handling, painting, drawing) runs on the UI thread.

## The Dispatcher

`Dispatcher` (`src/Jumbee.Console/Dispatcher.cs`) owns the UI thread and a serialized work queue.

- It runs one background `Thread` whose loop is: `wait(frameInterval) → drain the queue → run the frame callback`.
  A `ConcurrentQueue<Action>` holds posted work; an `AutoResetEvent` wakes the loop immediately when work is
  posted, and the `frameInterval` timeout wakes it periodically so animations advance even with no input.
- `Start(frame, frameIntervalMs)` blocks (via a startup barrier) until the loop has recorded its thread id, so
  `CheckAccess()` is correct the moment `Start` returns.
- A posted-action error or a frame error is caught and logged-by-omission — it never kills the UI thread.

Key members:

| Member | Purpose |
| :--- | :--- |
| `CheckAccess()` | `true` when the caller is on the UI thread — **or when no UI thread is running** (so headless/inline callers and tests "have access"). |
| `VerifyAccess()` | Throws if not on the UI thread. |
| `Post(Action)` | Enqueue work to run on the UI thread (fire-and-forget). |
| `Invoke(Action)` | Run on the UI thread, **blocking** until done; runs inline if already on the UI thread; rethrows exceptions on the caller. |
| `InvokeAsync(Action)` / `InvokeAsync<T>(Func<T>)` | Run on the UI thread, return a `Task`/`Task<T>`. |
| `InvokeAsync(Func<Task>)` / `InvokeAsync<T>(Func<Task<T>>)` | Run an **async** delegate on the UI thread and return a task that completes (unwrapped) when the inner work finishes, propagating exceptions. |
| `ThreadId`, `IsRunning` | Diagnostics. |

`UI` (`src/Jumbee.Console/UI.cs`) wires the dispatcher into the application and exposes the public surface:
`UI.Dispatcher`, `UI.CheckAccess()`, `UI.VerifyAccess()`, `UI.Post(...)`, and the `UI.InvokeAsync(...)`
overloads. `UI.Invoke(...)` is **public** — it is the marshaling primitive used by the library's own
controls; application code typically uses `UI.Post`/`UI.InvokeAsync`.

## The frame loop

Each frame, on the UI thread, `UI.OnFrame` runs (as the dispatcher's frame callback, after the queue has
been drained). The renderer is **dirty-rectangle**: it re-composites only the regions that changed, not the
whole screen. The order is **paint, then composite**:

1. `AdjustBufferSize()` — a cheap terminal-resize check (a resize marks the whole surface dirty via `Initialize`).
2. Fire the `Paint` event so each control renders its current state into its buffer. As a control finishes, it
   reports its own screen area as damaged: `Control.OnPaint` calls the ConsoleGUI base `Update(rect)`, whose rect
   bubbles up the `DrawingContext` tree (translated to screen coordinates) into a per-frame **dirty accumulator**
   in `ConsoleManager` (`_dirtyRects`, or `_fullDirty` for a whole-tree redraw).
3. Composite: if anything is dirty, `ConsoleManager.Draw()` → `FlushDirty()` re-composites the damaged region(s)
   — the whole screen on a full-dirty (startup/resize), else one `Update(rect)` per dirty rect. An **idle** frame
   (nothing dirtied) skips the scan entirely, so an idle UI does almost no work per frame. The metric
   `ProcessMetrics.DirtyAreaPercentAvg` reports the fraction of the screen re-composited per drawn frame.

Paint runs **before** composite so the composite reads freshly-painted buffers (not last frame's, or empty ones
on the first frame after startup/resize). The per-cell `ConsoleBuffer` diff is still the correctness backstop:
an over-large dirty rect only wastes scan time, it can never emit a wrong cell.

`UI.MarkDirty()` sets the redraw flag; `Control.Invalidate()` and `UI.Invoke` call it when state changes. Note
`Control.OnPaint` does **not** call it — the bubbled dirty rect is itself the redraw signal for that frame.

**Input forces a full redraw.** `UI.OnInput` marks the whole surface dirty (`ConsoleManager.MarkFullDirty()`) for
any *action* input — a key, click, wheel, or paste (not bare pointer motion) — so the whole screen is
re-composited on the frame that input is handled. This is a robustness guarantee: some controls request a repaint
but don't fully localize their damage through the partial-redraw path (they relied on the pre-dirty-rect
renderer, which redrew everything every frame), so a partial redraw can miss part of their change. Full-redrawing
on discrete input is essentially free at human rates; autonomous updates (animation, throttled self-refresh) and
pointer motion stay on the efficient partial-redraw path. *(Tightening those under-reporting controls so
interaction can also be a partial redraw is a future task — see the note in `ConsoleGUI Control Rendering`.)*

## Input flow

1. The input reader thread (`UI.InputLoop`) polls an `IInputSource` for keys.
2. For each key it calls `dispatcher.Post(() => OnInput(key))` — so **input is dispatched on the UI thread**.
3. `UI.OnInput` runs global hotkeys, then routes the key to the focused control's `OnInput`.

Because input runs on the UI thread, input handlers can read and mutate control state directly with no
locking.

`IInputSource` (`src/Jumbee.Console/Input/InputSource.cs`) abstracts the key source:

- `ConsoleInputSource` (default) reads `System.Console` (and tolerates redirected/unavailable input).
- Tests/headless/scripted scenarios supply their own (e.g. a queue-backed fake) via the `input` parameter of
  `UI.Start`.

## Marshaling: updating controls from a background thread

The whole point of the auto-marshal model is that you can update a control from any thread and it "just works":

```csharp
// From a background task (e.g. polling an RSS feed):
foreach (var item in await FetchFeedAsync())
    listBox.AddItem(item.Title);   // marshaled onto the UI thread internally
```

Under the hood `AddItem` runs its non-atomic mutation through `UI.Invoke`, which is:

```
if (UI.CheckAccess()) run inline;   // already on the UI thread
else dispatcher.Post(...);          // background thread -> deferred to the UI thread
```

Application code that needs to run arbitrary work on the UI thread uses the public API:

- `UI.Post(action)` — fire-and-forget.
- `await UI.InvokeAsync(action)` / `await UI.InvokeAsync(() => result)` — run and await.
- `await UI.InvokeAsync(async () => { ... })` — run an async delegate and await the **whole** operation.

> **Note:** marshaling makes off-thread updates **asynchronous** — they are applied on the next pump, not
> synchronously. Reading a property back immediately after setting it from another thread may see the old
> value until the dispatcher runs the change.

## async / await ergonomics

The UI thread installs a `SynchronizationContext` (`DispatcherSynchronizationContext`). Therefore, **any
`await` reached while running on the UI thread resumes back on the UI thread.** This covers input handlers,
the `onUpdate` callback, and anything started via `UI.Invoke`/`Post`/`InvokeAsync`.

So the idiomatic background-update pattern is a plain `async` method *started on the UI thread*:

```csharp
async Task PollFeeds()
{
    while (running)
    {
        var items = await FetchFeedAsync();   // resumes on the UI thread
        foreach (var i in items) list.AddItem(i.Title);  // safe: we're on the UI thread
        await Task.Delay(TimeSpan.FromMinutes(1));
    }
}

// Start it on the UI thread so its awaits resume there:
UI.Post(() => _ = PollFeeds());
```

`UI.InvokeAsync(async () => { ... })` binds to the `Func<Task>` overload (C# prefers it over `Action` for
async lambdas), so `await UI.InvokeAsync(async ...)` correctly waits for the full operation and propagates
exceptions — it is **not** fire-and-forget.

## Rules for control authors

These are the conventions that keep the single-threaded model correct. (See also the "Control implementation
considerations" in the root `CLAUDE.md`.)

1. **Request a redraw on any visual change.** Use `SetAtomicProperty` for simple scalar properties, or call
   `Invalidate()` directly. `Invalidate()` marks the control dirty for the next frame.

2. **Atomic scalar properties → `SetAtomicProperty`.**
   ```csharp
   public Color? SelectedForegroundColor
   {
       get => _selectedForegroundColor;
       set => SetAtomicProperty(ref _selectedForegroundColor, value);
   }
   ```
   `SetAtomicProperty(ref field, value, updatesLayout, onChanged)` does the equality check, assigns the field
   **directly on the calling thread**, runs an optional `onChanged`, then either `Initialize()` (when
   `updatesLayout: true`) or `Invalidate()`. The direct write relies on the assignment being atomic; multi-field
   structs (e.g. `Color?`) set from a background thread may briefly **tear** — a self-correcting one-frame risk
   that is accepted.

3. **Non-atomic / compound mutations → `UI.Invoke`.** Collections and mutations of a wrapped Spectre control are
   not atomically writable, so they must run on the UI thread:
   ```csharp
   public ListBoxItem AddItem(string text, ...)
   {
       var item = new ListBoxItem(this, Interlocked.Increment(ref _itemIndex), text, ...);
       UI.Invoke(() =>          // inline on the UI thread, posted from a background thread
       {
           _items[item.Index] = item;
           Invalidate();
       });
       return item;
   }
   ```
   Because the mutation is serialized onto the UI thread, controls use plain `Dictionary`/`List` — **not**
   `ConcurrentDictionary` — and `SpectreControl.UpdateContent` mutates the wrapped control **in place** (no
   copy-on-write).

4. **Layout/geometry changes go through `Initialize()` / `UI.Invoke`.** Setting a control's size, a frame's
   title, margins, scroll offset, etc. must run on the UI thread before the next redraw. `Control.Initialize()`
   already wraps its body in `UI.Invoke`.

5. **Reads of collection state should also be on the UI thread.** Because controls use non-concurrent
   collections, reading `Items`/`Count`/`Children` from a background thread can race with a UI-thread write.
   Read on the UI thread (e.g. via `UI.InvokeAsync(() => list.Items.Count)`).

6. **Do not add locks for UI state.** There is no UI lock; single-thread ownership is the synchronization.

### Return values and the deferred model

Because off-thread mutations are deferred, methods whose result depends on the mutation are only reliable when
called **on the UI thread**:

- `AddItem` returns a valid item immediately (the item and its index are created on the calling thread; only the
  collection insert is marshaled).
- `RemoveItem`'s `bool` result is accurate only when called on the UI thread (off-thread the removal is deferred,
  so it returns `false`).

## History

Earlier versions used a multi-threaded model with a shared `System.Threading.Lock`: controls acquired the lock
in paint/input, redraws only happened when the lock was free, and concurrent mutation was handled with
`ConcurrentDictionary` and copy-on-write (`CloneContent`/`UpdateContent`). That was replaced by the dispatcher
model in staged steps:

- **A** — introduced the `Dispatcher` (UI thread + queue) and moved the render loop and input onto it; the lock
  was kept temporarily.
- **B** — made `UI.Invoke` auto-marshal and removed the lock entirely (including `OnPaint`/`OnInput` locks and
  the `Lock` fields on the event args). Added the `Dispatcher.Start` startup barrier and the `IInputSource` /
  `IConsole` seams that make `UI.Start` testable.
- **C** — replaced `ConcurrentDictionary` with plain `Dictionary` and copy-on-write with in-place mutation in
  `ListBox`, `Tree`, `BarChart`, and `SpectreControl`, marshaling their mutations via `UI.Invoke`.
- **D** — installed the `SynchronizationContext` for the `await` ergonomic and added the `Func<Task>` /
  `Func<Task<T>>` `InvokeAsync` overloads.

The net effect: the shared lock, the deadlock-prone "don't take the lock in a setter" rules,
`ConcurrentDictionary`, and copy-on-write are all gone, while "mutate from any thread" survives as
auto-marshal.

## Testing the threading model

The threading model is testable without a real terminal:

- **Dispatcher in isolation** — start a `Dispatcher`, post/invoke work, assert it ran on the UI thread, that
  ordering is FIFO, that frames fire on the interval, and that `await` resumes on the UI thread
  (`tests/Jumbee.Console.Tests/DispatcherTests.cs`).
- **The live `UI.Start` loop** — start it with a headless `IConsole` (a `ConsoleBuffer` of fixed size) and a
  fake `IInputSource`; push keys and assert they reach the focused control
  (`tests/Jumbee.Console.Tests/UiStartTests.cs`). Redirect `Console.Out` to `TextWriter.Null` to swallow the
  ANSI output.
- **Concurrent mutation** — run mutations from several background threads under a live `UI.Start`, then read the
  final state **on the UI thread** via an `InvokeAsync` barrier (`UI.InvokeAsync(() => ...).Wait()`) and assert
  exact results — proving the mutations were serialized (`tests/Jumbee.Console.Tests/ConcurrencyTests.cs`).

The `InvokeAsync` barrier (posting an empty/observing action and waiting for it) is the standard way to wait
until all previously-posted work has been processed before asserting.

## Future work

Correctness currently depends on control authors using `SetAtomicProperty` / `UI.Invoke` in their setters. A
future milestone may add a **Roslyn analyzer** that flags control setters which mutate state without going
through these — keeping the simple hand-written setters while catching mistakes at compile time. (A source
generator that emits the setters was considered but rejected in favor of the analyzer's simplicity.)
