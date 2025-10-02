using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace MathMax.ChangeTracking;

/// <summary>
/// Extension methods that form the fluent DSL for declaring keyed collection tracking.
/// </summary>
public static class TrackByExtensions
{
    /// <summary>
    /// Declares that the target <paramref name="collection"/> should be diffed by the identity produced by <paramref name="keySelector"/>.
    /// An optional <paramref name="children"/> callback may declare further nested collection tracking for the element type.
    /// This method is a compile-time marker; it performs no runtime logic.
    /// </summary>
    public static void TrackBy<TElement, TKey>(
        this IEnumerable<TElement> collection,
        Expression<Func<TElement, TKey>> keySelector,
        Action<TElement>? children)
        where TElement : class
    {
        children?.Invoke(default!);
    }

    /// <summary>
    /// Overload without nested children declaration.
    /// </summary>
    public static void TrackBy<TElement, TKey>(
        this IEnumerable<TElement> collection,
        Expression<Func<TElement, TKey>> keySelector)
        where TElement : class
        => TrackBy(collection, keySelector, null);
}
