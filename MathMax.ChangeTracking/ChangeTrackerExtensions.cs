using System;
using System.Collections.Generic;
using System.Linq;

namespace MathMax.ChangeTracking;

public static class ChangeTrackerExtensions
{
    /// <summary>
    /// Creates a property difference record. Indicates a change in value of a property.
    /// </summary>
    /// <param name="path">The path to the property.</param>
    /// <param name="propertyName">The name of the property.</param>
    /// <param name="leftOwner">The owner of the property on the left side.</param>
    /// <param name="rightOwner">The owner of the property on the right side.</param>
    /// <param name="leftValue">The value of the property on the left side.</param>
    /// <param name="rightValue">The value of the property on the right side.</param>
    /// <returns>A property difference record.</returns>
    public static Difference CreatePropertyDifference(string path, string propertyName, object? leftOwner, object? rightOwner, object? leftValue, object? rightValue) => new()
    {
        Path = $"{path}.{propertyName}",
        Kind = DifferenceKind.PropertyValue,
        LeftOwner = leftOwner,
        RightOwner = rightOwner,
        LeftValue = leftValue,
        RightValue = rightValue
    };

    /// <summary>
    /// Creates a presence difference record (indicating addition or removal of an item).
    /// </summary>
    /// <param name="path">The path to the item.</param>
    /// <param name="left">The item on the left side.</param>
    /// <param name="right">The item on the right side.</param>
    /// <returns>A presence difference record.</returns>
    public static Difference CreatePresenceDifference(string path, object? parentOwner, object? leftValue, object? rightValue) => new()
    {
        Path = path,
        Kind = DifferenceKind.Presence,
        LeftOwner = parentOwner,
        RightOwner = parentOwner,
        LeftValue = leftValue,
        RightValue = rightValue
    };

    /// <summary>
    /// Diffs two lists by identity, producing presence differences for added/removed items and
    /// using the provided per-item diff function for items present in both lists. The order of
    /// items in the output is determined by the order of their first appearance in either input list
    /// </summary>
    /// <typeparam name="T">The type of items in the lists.</typeparam>
    /// <typeparam name="TKey">The type of the key used to identify items.</typeparam>
    /// <param name="leftList">The left list.</param>
    /// <param name="rightList">The right list.</param>
    /// <param name="path">The path to the list.</param>
    /// <param name="perItemDiff">A function to diff two items of type T, given their path.</param>
    /// <param name="keySelector">A function to select the key from an item of type T.</param>
    /// <returns>A sequence of differences between the two lists.</returns>
    public static IEnumerable<Difference> DiffListByIdentity<T, TKey>(IEnumerable<T> leftList, IEnumerable<T> rightList, string path,
        Func<T, T, string, IEnumerable<Difference>> perItemDiff,
        Func<T, TKey> keySelector,
        object? parentOwner = null)
            where TKey : notnull
            where T : class
    {
        var leftMap = leftList.ToDictionary(keySelector, v => v);
        var rightMap = rightList.ToDictionary(keySelector, v => v);
        var orderedKeys = GetOrderedKeys(leftList, rightList, keySelector);

        foreach (var key in orderedKeys)
        {
            var itemPath = path + "[" + key + "]";
            bool inLeft = leftMap.TryGetValue(key, out var left);
            bool inRight = rightMap.TryGetValue(key, out var right);

            if (left == null || right == null || !inLeft || !inRight)
            {
                // Parent owner is the collection (passed via parentOwner); if unknown, fall back to null.
                yield return CreatePresenceDifference(itemPath, parentOwner, left, right);
                continue;
            }

            foreach (var d in perItemDiff(left, right, itemPath))
            {
                yield return d;
            }
        }
    }

    private static IEnumerable<TKey> GetOrderedKeys<T, TKey>(IEnumerable<T> leftList, IEnumerable<T> rightList, Func<T, TKey> keySelector) where TKey : notnull
    {
        var seen = new HashSet<TKey>();
        foreach (var k in leftList.Select(keySelector).Where(k => seen.Add(k))) yield return k;
        foreach (var k in rightList.Select(keySelector).Where(k => seen.Add(k))) yield return k;
    }
}