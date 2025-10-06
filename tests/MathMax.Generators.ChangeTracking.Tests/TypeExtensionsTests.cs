using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.IO;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;
using MathMax.Generators.ChangeTracking;

namespace MathMax.Generators.ChangeTracking.Tests;

/// <summary>
/// Tests for the <see cref="TypeExtensions"/> helper methods.
/// These tests create an in-memory Roslyn compilation to obtain <see cref="ITypeSymbol"/> instances
/// with correct nullable annotations and interface implementations.
/// </summary>
public class TypeExtensionsTests
{
    private static class TestModel
    {
        private static readonly Lazy<(CSharpCompilation Compilation, INamedTypeSymbol Holder, IReadOnlyDictionary<string, IFieldSymbol> Fields)> _model
            = new(() => RoslynModelLoader.LoadFromFile(Path.Combine("Resources", "HolderModel.cs.txt"), "Holder"));

        public static ITypeSymbol T(string fieldName) => _model.Value.Fields[fieldName].Type;
    }

    public static IEnumerable<object[]> SimpleTypes() =>
    [
        ["NonNullableIntField"],
        ["NullableIntField"],
        ["NonNullableStringField"],
        ["NullableStringField"],
        ["GuidField"],
        ["DateTimeOffsetField"],
        ["TimeSpanField"],
        ["EnumField"],
    ];

    public static IEnumerable<object[]> CollectionTypes() =>
    [
        ["IntArrayField"],
        ["IntListField"],
        ["IntEnumerableField"],
        ["DictField"],
        ["IterableCustomField"],
    ];

    public static IEnumerable<object[]> ComplexTypes() =>
    [
        ["CustomClassField"],
        ["CustomStructField"],
    ];

    [Theory]
    [MemberData(nameof(SimpleTypes))]
    public void IsSimpleType_ReturnsTrue_ForExpectedTypes(string field)
    {
        Assert.True(TestModel.T(field).IsSimpleType(), $"Expected {field} to be simple");
    }

    [Theory]
    [MemberData(nameof(ComplexTypes))]
    public void IsSimpleType_ReturnsFalse_ForComplex(string field)
    {
        Assert.False(TestModel.T(field).IsSimpleType(), $"Expected {field} NOT to be simple");
    }

    [Theory]
    [MemberData(nameof(CollectionTypes))]
    public void IsCollectionType_ReturnsTrue(string field)
    {
        Assert.True(TestModel.T(field).IsCollectionType(), $"Expected {field} to be collection");
    }

    [Theory]
    [MemberData(nameof(SimpleTypes))]
    [MemberData(nameof(ComplexTypes))]
    public void IsCollectionType_ReturnsFalse_ForNonCollections(string field)
    {
        Assert.False(TestModel.T(field).IsCollectionType(), $"Expected {field} NOT to be collection");
    }

    [Theory]
    [MemberData(nameof(ComplexTypes))]
    public void IsComplexType_ReturnsTrue_ForComplex(string field)
    {
        Assert.True(TestModel.T(field).IsComplexType(), $"Expected {field} to be complex");
    }

    [Theory]
    [MemberData(nameof(SimpleTypes))]
    [MemberData(nameof(CollectionTypes))]
    public void IsComplexType_ReturnsFalse_ForNonComplex(string field)
    {
        Assert.False(TestModel.T(field).IsComplexType(), $"Expected {field} NOT to be complex");
    }

    [Fact]
    public void IsNullable_DetectsNullableReferenceAndValueTypes()
    {
        Assert.True(TestModel.T("NullableStringField").IsNullable());
        Assert.True(TestModel.T("NullableIntField").IsNullable());
        Assert.False(TestModel.T("NonNullableStringField").IsNullable());
        Assert.False(TestModel.T("NonNullableIntField").IsNullable());
    }

    [Fact]
    public void UnwrapNullable_ReturnsUnderlyingType()
    {
        var nullableInt = TestModel.T("NullableIntField");
        var intType = TestModel.T("NonNullableIntField");
        Assert.Equal(intType, nullableInt.UnwrapNullable());
        Assert.Equal(intType, intType.UnwrapNullable()); // idempotent for non-nullable
    }

    [Fact]
    public void ReferenceAndValueTypeChecks()
    {
        Assert.True(TestModel.T("NonNullableStringField").IsReferenceType());
        Assert.True(TestModel.T("CustomClassField").IsReferenceType());
        Assert.False(TestModel.T("NonNullableIntField").IsReferenceType());
        Assert.False(TestModel.T("CustomStructField").IsReferenceType());

        Assert.True(TestModel.T("NonNullableIntField").IsValueType());
        Assert.True(TestModel.T("CustomStructField").IsValueType());
        Assert.True(TestModel.T("EnumField").IsValueType());
        Assert.False(TestModel.T("NonNullableStringField").IsValueType());
        Assert.False(TestModel.T("CustomClassField").IsValueType());
    }

    [Fact]
    public void GetTypeCategory_Simple()
    {
        foreach (var f in SimpleTypes().Select(a => (string)a[0]))
        {
            Assert.Equal(TypeCategory.Simple, TestModel.T(f).GetTypeCategory());
        }
    }

    [Fact]
    public void GetTypeCategory_Collection()
    {
        foreach (var f in CollectionTypes().Select(a => (string)a[0]))
        {
            Assert.Equal(TypeCategory.Collection, TestModel.T(f).GetTypeCategory());
        }
    }

    [Fact]
    public void GetTypeCategory_Complex()
    {
        foreach (var f in ComplexTypes().Select(a => (string)a[0]))
        {
            Assert.Equal(TypeCategory.Complex, TestModel.T(f).GetTypeCategory());
        }
    }

    [Fact]
    public void GetTypeCategory_Unknown_ForNull()
    {
        ITypeSymbol? none = null;
        Assert.Equal(TypeCategory.Unknown, none.GetTypeCategory());
    }
}

internal sealed class MetadataReferenceComparer : IEqualityComparer<MetadataReference>
{
    public static MetadataReferenceComparer Instance { get; } = new();
    public bool Equals(MetadataReference? x, MetadataReference? y) => ReferenceEquals(x, y) || (x is not null && y is not null && x.Display == y.Display);
    public int GetHashCode(MetadataReference obj) => obj.Display?.GetHashCode() ?? 0;
}
