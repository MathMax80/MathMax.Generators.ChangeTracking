using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

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
        LeftOwner = leftOwner,
        RightOwner = rightOwner,
        LeftValue = leftValue,
        RightValue = rightValue,
        Kind = DifferenceKind.Modification
    };

    /// <summary>
    /// Creates a presence difference record (indicating addition or removal of an item).
    /// </summary>
    /// <param name="path">The path to the item.</param>
    /// <param name="left">The item on the left side.</param>
    /// <param name="right">The item on the right side.</param>
    /// <returns>A presence difference record.</returns>
    public static Difference CreatePresenceDifference(string path, object? left, object? right)
    {
        var kind = DifferenceKind.Modification;
        if (left == null && right != null) kind = DifferenceKind.Addition;
        else if (right == null && left != null) kind = DifferenceKind.Removal;

        return new Difference
        {
            Path = path,
            LeftOwner = left,
            RightOwner = right,
            LeftValue = left,
            RightValue = right,
            Kind = kind
        };
    }

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
        Expression<Func<T, TKey>> keySelectorExpr)
            where TKey : notnull
            where T : class
    {
        var keySelector = keySelectorExpr.Compile();
        var leftMap = leftList.ToDictionary(keySelector, v => v);
        var rightMap = rightList.ToDictionary(keySelector, v => v);
        var uniqueKeys = GetUniqueKeys(leftList, rightList, keySelector);
        string[] memberNames = GetKeyMembers(keySelectorExpr);

        foreach (var key in uniqueKeys)
        {
            string keyString = FormatKey(key, memberNames);
            var itemPath = path + "[" + keyString + "]";
            bool inLeft = leftMap.TryGetValue(key, out var left);
            bool inRight = rightMap.TryGetValue(key, out var right);

            if (left == null || right == null || !inLeft || !inRight)
            {
                // Owners are the parent collections (leftList/rightList) – callers supply those.
                var kind = DifferenceKind.Modification;

                if (left == null && right != null)
                {
                    kind = DifferenceKind.Addition;
                }
                else if (right == null && left != null)
                {
                    kind = DifferenceKind.Removal;
                }

                var diff = new Difference
                {
                    Path = itemPath,
                    LeftOwner = leftList,
                    RightOwner = rightList,
                    LeftValue = left,
                    RightValue = right,
                    Kind = kind
                };
                yield return diff;
                continue;
            }

            foreach (var difference in perItemDiff(left, right, itemPath))
            {
                yield return difference;
            }
        }
    }

    private static string[] GetKeyMembers<T, TKey>(Expression<Func<T, TKey>> keySelectorExpr)
        where T : class
        where TKey : notnull
    {

        // Pre-compute key member metadata for formatting
        if (keySelectorExpr.Body is MemberExpression member)
        {
            return [member.Member.Name];
        }

        if (keySelectorExpr.Body is NewExpression newExpression && newExpression.Members != null)
        {
            return [.. newExpression.Members.Select(m => m.Name)];
        }

        throw new ChangeTrackingGenerationException("Unsupported key selector expression. Must be a member access or anonymous object creation.");
    }

    private static IEnumerable<TKey> GetUniqueKeys<T, TKey>(IEnumerable<T> leftList, IEnumerable<T> rightList, Func<T, TKey> keySelector) where TKey : notnull
    {
        var uniqueKeys = new HashSet<TKey>();
        foreach (var key in leftList.Select(keySelector).Where(uniqueKeys.Add))
        {
            yield return key;
        }

        foreach (var key in rightList.Select(keySelector).Where(uniqueKeys.Add))
        {
            yield return key;
        }
    }

    /// <summary>
    /// Formats a key value for display in a path segment. 
    /// Handles single-value keys and anonymous composite keys. 
    /// </summary>
    /// <typeparam name="TKey">The type of the key to format.</typeparam>
    /// <param name="key">The key value to format.</param>
    /// <param name="memberNames">The names of the key members for composite keys.</param>
    /// <returns>A formatted string representation of the key suitable for path segments.</returns>
    private static string FormatKey<TKey>(TKey key, string[] memberNames)
        where TKey : notnull
    {
        return memberNames.Length switch
        {
            0 => FormatSimpleKey(key),
            1 => FormatSingleMemberKey(key, memberNames[0]),
            _ => FormatCompositeMemberKey(key, memberNames)
        };
    }

    private static string FormatSimpleKey<TKey>(TKey key) where TKey : notnull
    {
        var keyString = key.ToString() ?? key.GetType().Name;
        return $"({keyString})";
    }

    private static string FormatSingleMemberKey<TKey>(TKey key, string memberName) where TKey : notnull
    {
        var keyString = key.ToString() ?? key.GetType().Name;
        var quotedValue = QuoteIfNeeded(keyString, key.GetType());
        return $"({memberName}={quotedValue})";
    }

    private static string FormatCompositeMemberKey<TKey>(TKey key, string[] memberNames) where TKey : notnull
    {
        var keyType = key.GetType();
        var formattedPairs = new string[memberNames.Length];
        
        for (int i = 0; i < memberNames.Length; i++)
        {
            var memberName = memberNames[i];
            var property = keyType.GetProperty(memberName);
            
            if (property == null)
            {
                throw new ChangeTrackingGenerationException($"Property '{memberName}' not found on type '{keyType.Name}'.");
            }

            var propertyValue = property.GetValue(key);
            var quotedValue = QuoteIfNeeded(propertyValue, property.PropertyType);
            formattedPairs[i] = $"{memberName}={quotedValue}";
        }
        
        return $"({string.Join(",", formattedPairs)})";
    }

    private static string QuoteIfNeeded<T>(T? value, Type? type)
    {
        if (value is null)
        {
            return "";
        }

        // Quote string and char types for clarity in output
        if (type == typeof(string))
        {
            return $"'{value}'";
        }
        
        if (type == typeof(char))
        {
            return $"'{value}'";
        }

        // For other types, use their string representation
        return value.ToString() ?? value.GetType().Name;
    }
}