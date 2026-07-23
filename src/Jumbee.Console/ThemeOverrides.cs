namespace Jumbee.Console;

using System.Collections.Generic;

/// <summary>
/// Tracks which theme-derived properties a caller has explicitly overridden, keyed by property name, so a
/// runtime theme switch (an <c>ApplyTheme</c>) can re-apply theme defaults <em>only</em> to the properties the
/// caller has not customised. A property is marked when its themeable setter runs (or a non-null themeable
/// constructor argument is supplied). Shared so the same technique can back control-level overrides later.
/// </summary>
internal sealed class ThemeOverrides
{
    private readonly Dictionary<string, bool> _overridden = new();

    /// <summary>Records that <paramref name="property"/> was explicitly set by the caller.</summary>
    public void Mark(string property) => _overridden[property] = true;

    /// <summary><see langword="true"/> if <paramref name="property"/> was explicitly overridden.</summary>
    public bool IsOverridden(string property) => _overridden.TryGetValue(property, out var v) && v;
}