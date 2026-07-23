using Spectre.Console.Rendering;

namespace Spectre.Console.Interop;

internal static class SegmentExtensions
{
    /// <summary>
    /// Returns a copy of the segment with its <see cref="Segment.Style"/> replaced by <paramref name="style"/>,
    /// keeping its text.
    /// </summary>
    /// <remarks>
    /// Line-break and control-code segments (which carry no visible glyph to restyle) are returned unchanged. Handy
    /// for overlaying a highlight/selection style onto already-rendered <see cref="Segment"/>s.
    /// </remarks>
    public static Segment WithStyle(this Segment segment, Style style) =>
        segment.IsLineBreak || segment.IsControlCode ? segment : new Segment(segment.Text, style);
}

internal static class ListExtensions
{
    public static void RemoveLast<T>(this List<T> list)
    {
        ArgumentNullException.ThrowIfNull(list);

        if (list.Count > 0)
        {
            list.RemoveAt(list.Count - 1);
        }
    }

    public static void AddOrReplaceLast<T>(this List<T> list, T item)
    {
        ArgumentNullException.ThrowIfNull(list);

        if (list.Count == 0)
        {
            list.Add(item);
        }
        else
        {
            list[list.Count - 1] = item;
        }
    }
}

internal static class EnumerableExtensions
{
    // List.Reverse clashes with IEnumerable<T>.Reverse, so this method only exists
    // so we won't have to cast List<T> to IEnumerable<T>.
    public static IEnumerable<T> ReverseEnumerable<T>(this IEnumerable<T> source)
    {
        ArgumentNullException.ThrowIfNull(source);

        return source.Reverse();
    }

    public static bool None<T>(this IEnumerable<T> source, Func<T, bool> predicate) => !source.Any(predicate);

    public static IEnumerable<T> Repeat<T>(this IEnumerable<T> source, int count)
    {
        while (count-- > 0)
        {
            foreach (var item in source)
            {
                yield return item;
            }
        }
    }

    public static int IndexOf<T>(this IEnumerable<T> source, T item)
    {
        var index = 0;
        foreach (var candidate in source)
        {
            if (Equals(candidate, item))
            {
                return index;
            }

            index++;
        }

        return -1;
    }

    public static int GetCount<T>(this IEnumerable<T> source)
    {
        if (source is IList<T> list)
        {
            return list.Count;
        }

        return source is T[] array ? array.Length : source.Count();
    }

    public static void ForEach<T>(this IEnumerable<T> source, Action<T> action)
    {
        foreach (var item in source)
        {
            action(item);
        }
    }

    public static bool AnyTrue(this IEnumerable<bool> source) => source.Any(b => b);

    public static IEnumerable<(int Index, bool First, bool Last, T Item)> Enumerate<T>(this IEnumerable<T> source)
    {
        ArgumentNullException.ThrowIfNull(source);

        return Enumerate(source.GetEnumerator());
    }

    public static IEnumerable<(int Index, bool First, bool Last, T Item)> Enumerate<T>(this IEnumerator<T> source)
    {
        ArgumentNullException.ThrowIfNull(source);

        var first = true;
        var last = !source.MoveNext();
        T current;

        for (var index = 0; !last; index++)
        {
            current = source.Current;
            last = !source.MoveNext();
            yield return (index, first, last, current);
            first = false;
        }
    }

    public static IEnumerable<TResult> SelectIndex<T, TResult>(this IEnumerable<T> source, Func<T, int, TResult> func) => source.Select((value, index) => func(value, index));

#if !NET6_0_OR_GREATER
    public static IEnumerable<(TFirst First, TSecond Second)> Zip<TFirst, TSecond>(
        this IEnumerable<TFirst> source, IEnumerable<TSecond> first)
    {
        return source.Zip(first, (first, second) => (first, second));
    }
#endif

    public static IEnumerable<(TFirst First, TSecond Second, TThird Third)> ZipThree<TFirst, TSecond, TThird>(
        this IEnumerable<TFirst> first, IEnumerable<TSecond> second, IEnumerable<TThird> third) => first.Zip(second, (a, b) => (a, b))
            .Zip(third, (a, b) => (a.a, a.b, b));
}

internal static class StyleExtensions
{
    public static ConsoleGUI.Data.Color? ToConsoleGUIColor(this Color color) => color == Color.Default ? null : new ConsoleGUI.Data.Color(color.R, color.G, color.B);
}

/// <summary>
/// Indicates that the tree being rendered includes a cycle, and cannot be rendered.
/// </summary>
public sealed class CircularTreeException : Exception
{
    internal CircularTreeException(string message)
        : base(message)
    {
    }
}