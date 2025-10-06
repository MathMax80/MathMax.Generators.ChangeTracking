using System.Linq;
using Microsoft.CodeAnalysis;

namespace MathMax.Generators.ChangeTracking;

/// <summary>
/// High-level classification buckets for types used by change tracking generation.
/// </summary>
public enum TypeCategory
{
    /// <summary>Type was null / could not be determined.</summary>
    Unknown = 0,
    /// <summary>Scalar / primitive-like leaf value (no recursive traversal).</summary>
    Simple,
    /// <summary>Enumerable/collection of elements.</summary>
    Collection,
    /// <summary>Non-simple, non-collection object that should be traversed (e.g., user-defined class/struct).</summary>
    Complex
}

/// <summary>
/// Helpers for classifying <see cref="ITypeSymbol"/>s inside the source generator.
/// </summary>
public static class TypeExtensions
{
    /// <summary>
    /// Determines whether the type symbol represents a nullable type (nullable reference or Nullable&lt;T&gt; value type).
    /// </summary>
    public static bool IsNullable(this ITypeSymbol? type)
    {
        if (type is null)
        {
            return false;
        }

        // Nullable reference type annotation
        if (type.IsReferenceType && type.NullableAnnotation == NullableAnnotation.Annotated)
        {
            return true;
        }

        // Nullable value type Nullable<T>
        return type is INamedTypeSymbol { OriginalDefinition.SpecialType: SpecialType.System_Nullable_T };
    }

    /// <summary>
    /// Returns the underlying type if the symbol is Nullable&lt;T&gt;; otherwise returns the symbol itself.
    /// </summary>
    public static ITypeSymbol? UnwrapNullable(this ITypeSymbol? type)
        => type is INamedTypeSymbol { OriginalDefinition.SpecialType: SpecialType.System_Nullable_T } named
            ? named.TypeArguments[0]
            : type;

    /// <summary>
    /// Determines whether the (possibly nullable) type should be treated as a simple/primitive-like value.
    /// Includes enums, primitives, string, Guid, DateTimeOffset, TimeSpan, and IntPtr/UIntPtr.
    /// Nullable wrappers of these count as simple.
    /// </summary>
    public static bool IsSimpleType(this ITypeSymbol? type)
    {
        if (type is null)
        {
            return false;
        }

        type = type.UnwrapNullable();

        if (type is null)
        {
            return false;
        }

        if (type.TypeKind == TypeKind.Enum)
        {
            return true;
        }

        switch (type.SpecialType)
        {
            case SpecialType.System_Boolean:
            case SpecialType.System_Byte:
            case SpecialType.System_Char:
            case SpecialType.System_DateTime:
            case SpecialType.System_Decimal:
            case SpecialType.System_Double:
            case SpecialType.System_Int16:
            case SpecialType.System_Int32:
            case SpecialType.System_Int64:
            case SpecialType.System_SByte:
            case SpecialType.System_Single:
            case SpecialType.System_String:
            case SpecialType.System_UInt16:
            case SpecialType.System_UInt32:
            case SpecialType.System_UInt64:
            case SpecialType.System_IntPtr:
            case SpecialType.System_UIntPtr:
                return true;
        }

        // Additional "primitive-like" structs commonly treated as simple value objects.
        var display = type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        return display is "global::System.Guid" or "global::System.DateTimeOffset" or "global::System.TimeSpan";
    }

    /// <summary>
    /// Determines whether the type symbol represents a collection (array, IEnumerable, IEnumerable&lt;T&gt; or anything that implements those).
    /// </summary>
    public static bool IsCollectionType(this ITypeSymbol? type)
    {
        if (type is null)
        {
            return false;
        }

        // Treat System.String specially: although it implements IEnumerable<char>, we consider it a simple scalar value
        // for change tracking purposes, not a collection to traverse.
        if (type.SpecialType == SpecialType.System_String)
        {
            return false;
        }

        if (type.TypeKind == TypeKind.Array)
        {
            return true;
        }

        // If the symbol itself is IEnumerable<T>
        if (type is INamedTypeSymbol named &&
            named.OriginalDefinition.SpecialType == SpecialType.System_Collections_Generic_IEnumerable_T)
        {
            return true;
        }

        // Non-generic IEnumerable
        if (type.SpecialType == SpecialType.System_Collections_IEnumerable)
        {
            return true;
        }

        // Any implemented interface that is IEnumerable or IEnumerable<T>
        foreach (var interfaceSymbol in type.AllInterfaces)
        {
            if (interfaceSymbol.SpecialType == SpecialType.System_Collections_IEnumerable)
            {
                return true;
            }

            if (interfaceSymbol is INamedTypeSymbol { OriginalDefinition.SpecialType: SpecialType.System_Collections_Generic_IEnumerable_T })
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Determines if a type is considered complex: not simple and not a collection.
    /// </summary>
    public static bool IsComplexType(this ITypeSymbol? type) => type is not null && !type.IsSimpleType() && !type.IsCollectionType();

    public static bool IsReferenceType(this ITypeSymbol? type) => type?.IsReferenceType == true;

    public static bool IsValueType(this ITypeSymbol? type) => type?.IsValueType == true;

    /// <summary>
    /// Classifies a type into a single <see cref="TypeCategory"/>. Order of precedence:
    /// 1. Simple 2. Collection 3. Complex. Other and Null yields Unknown.
    /// </summary>
    public static TypeCategory GetTypeCategory(this ITypeSymbol? type)
    {
        if (type is null)
        {
            return TypeCategory.Unknown;
        }

        if (type.IsSimpleType())
        {
            return TypeCategory.Simple;
        }

        if (type.IsCollectionType())
        {
            return TypeCategory.Collection;
        }

        if (type.IsComplexType())
        {
            return TypeCategory.Complex;
        }

        return TypeCategory.Unknown;
    }
}