namespace Jumbee.Console.Examples;

using System;
using System.Collections.Generic;
using System.Threading;

/// <summary>
/// Marks a control (or layout) as a browsable example: it <em>is</em> the live content shown in the middle pane
/// (<c>IExample : IFocusable</c>), plus the metadata the tree/header use and the source files the viewer shows. So an
/// example is just a normal control that also carries a little metadata — e.g. <c>class ListBoxExample : ListBox,
/// IExample</c>. <see cref="SourceFiles"/> defaults to the example's own <c>.cs</c> file, plus — when the example
/// derives from a real Jumbee control — that control's source, so the viewer shows both.
/// </summary>
public interface IExample : IFocusable
{
    /// <summary>Tree grouping, e.g. "Flexibility" or "Controls".</summary>
    string Category { get; }

    /// <summary>Tree label and middle-pane frame title.</summary>
    string Title { get; }

    /// <summary>One-line summary shown above the demo.</summary>
    string Description { get; }

    /// <summary>
    /// Source file names shown as read-only tabs in the right pane; resolved against the embedded resources by
    /// <see cref="SourceLoader"/>. Defaults to this type's own <c>.cs</c> file and, when the example is built on a real
    /// Jumbee control (not a framework base like <c>Control</c>/<c>CompositeControl</c>), that base control's <c>.cs</c>
    /// too — so a user can read both the demo and the control it uses.
    /// </summary>
    IReadOnlyList<string> SourceFiles
    {
        get
        {
            var own = GetType().Name + ".cs";
            var baseType = GetType().BaseType;
            if (baseType is not null && baseType != typeof(object))
            {
                var name = baseType.Name.Split('`')[0];   // strip generic arity, e.g. "SpectreControl`1" -> "SpectreControl"
                bool frameworkBase = name is "Control" or "CompositeControl" or "RenderableControl" or "Layout";
                if (!frameworkBase && SourceLoader.Exists(name + ".cs"))
                    return [own, name + ".cs"];
            }
            return [own];
        }
    }

    /// <summary>When <see langword="true"/>, the example re-fits itself to the pane and must not be scrolled (e.g.
    /// <see cref="Plot"/>): the host fills the frame viewport instead of giving the example unbounded scroll height
    /// (which would balloon a fill-to-viewport control to the size clamp). Defaults to <see langword="false"/> — the
    /// normal scrollable example.</summary>
    bool FillsPane => false;
}

/// <summary>
/// An example that runs background feeds (timers, live data) while it is the shown example. One interface to
/// implement: it <em>is</em> an <see cref="IExample"/>. Start the feeds in <see cref="OnActivated"/> and record their
/// <see cref="CancellationTokenSource"/> handles in <see cref="FeedTasks"/>; the default <see cref="OnDeactivated"/>
/// cancels them all, so most examples need no teardown code. <see cref="ExampleHost"/> calls
/// <see cref="OnActivated"/>/<see cref="OnDeactivated"/> as the example is shown/replaced, so a feed only runs while
/// visible.
/// </summary>
public interface IActivatableExample : IExample
{
    /// <summary>The cancellation handles of the feeds started while shown (populate in <see cref="OnActivated"/>).
    /// The default <see cref="OnDeactivated"/> cancels each of these. Defaults to empty.</summary>
    IReadOnlyList<CancellationTokenSource> FeedTasks => [];

    /// <summary>Called when the example becomes the shown one — start its feeds and record their handles in
    /// <see cref="FeedTasks"/>.</summary>
    void OnActivated();

    /// <summary>Called when the example is replaced. The default cancels every <see cref="FeedTasks"/> handle; override
    /// only for extra teardown.</summary>
    void OnDeactivated()
    {
        foreach (var feed in FeedTasks) feed?.Cancel();
    }
}
