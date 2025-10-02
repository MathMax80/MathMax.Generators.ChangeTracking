using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace MathMax.ChangeTracking;

/// <summary>
/// Entry point for declaring change tracking maps using a lightweight DSL.
/// The <see cref="Map{TRoot}"/> method is a compile-time only construct inspected by the source generator; it has no runtime behavior.
/// </summary>
public static class ChangeTracking
{
    /// <summary>
    /// Defines a change tracking map for the specified root entity type. The <paramref name="map"/> action
    /// should reference collection navigation properties and invoke <see cref="TrackByExtensions.TrackBy"/>
    /// to declare keyed collection tracking. Nested collections are declared via the optional children lambda
    /// parameter on <c>TrackBy</c> and appear as additional <c>TrackBy</c> calls inside that nested scope.
    /// </summary>
    /// <typeparam name="TRoot">Root aggregate/entity type.</typeparam>
    /// <param name="map">Lambda whose single parameter symbolically represents an instance of <typeparamref name="TRoot"/>.</param>
    public static void Map<TRoot>(Action<TRoot> map)
        where TRoot : class
    {
        // Intentionally execute the lambda with a default value so that any nested lambdas are available in the syntax tree.
        // No runtime state is captured; the generator inspects the source only.
        map(default!);
    }
}

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
