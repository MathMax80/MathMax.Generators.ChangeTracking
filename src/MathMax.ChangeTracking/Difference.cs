using System.Text.Json.Serialization;

namespace MathMax.ChangeTracking;

/// <summary>
/// Represents a single difference between two object graphs compared by the generated Diff extensions.
/// Contains information about the property or collection element that was added, removed, or modified.
/// </summary>
public sealed class Difference
{
    /// <summary>
    /// The location of the changed property or object within the object graph.
    /// For scalar properties, this is the property name (e.g., <c>FirstName</c>, <c>Address.ZipCode</c>).
    /// For collections, the path uses entity IDs rather than indexes and includes added or removed items
    /// (e.g., <c>Orders[OrderId=123]</c> for an added/removed order,
    /// <c>Orders[OrderId=123].Items[Id=17].Quantity</c> for a modified nested property).
    /// This path is human-readable, deterministic, and allows consumers to locate changes precisely,
    /// including additions, removals, and modifications in collections.
    /// </summary>
    /// <remarks>
    /// The <see cref="DifferenceKind"/> property can be used to quickly detect whether the difference is an addition or removal,
    /// since this cannot be determined from the path alone.
    /// Correspondingly, <see cref="LeftValue"/> will be <c>null</c> for additions, and <see cref="RightValue"/> will be <c>null</c> for removals.
    /// </remarks>
    public string Path { get; set; } = string.Empty;

    /// <summary>
    /// The object instance on the left side (original) that owns the property or collection element indicated by <see cref="Path"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// For scalar properties, this is the parent object containing the changed property.
    /// For example, if <c>Path</c> is <c>Address.ZipCode</c>, <see cref="LeftOwner"/> will be the <see cref="Address"/> instance on the left side.
    /// </para>
    /// <para>
    /// For collection changes, <see cref="LeftOwner"/> represents the collection instance on the left that contains the changed element.
    /// For example, if an <c>OrderItem</c> was removed from <c>Orders[OrderId=123].Items</c>, <see cref="LeftOwner"/> will be the <c>Items</c> collection of that order on the left.
    /// </para>
    /// <para>
    /// If the owning object or collection did not exist on the left (e.g., the entire object or collection was added), <see cref="LeftOwner"/> will be <c>null</c>.
    /// </para>
    /// <para>
    /// <see cref="LeftOwner"/> can be used with <see cref="DifferenceKind"/> and <see cref="LeftValue"/> to inspect or revert changes on the original object graph.
    /// </para>
    /// </remarks>
    [JsonIgnore]
    public object? LeftOwner { get; set; }

    /// <summary>
    /// The object instance on the right side (new/modified) that owns the property or collection element indicated by <see cref="Path"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// For scalar properties, this is the parent object containing the changed property.
    /// For example, if <c>Path</c> is <c>Address.ZipCode</c>, <see cref="RightOwner"/> will be the <see cref="Address"/> instance on the right side.
    /// </para>
    /// <para>
    /// For collection changes, <see cref="RightOwner"/> represents the collection instance on the right that contains the changed element.
    /// For example, if an <c>OrderItem</c> was added to <c>Orders[OrderId=123].Items</c>, <see cref="RightOwner"/> will be the <c>Items</c> collection of that order on the right.
    /// </para>
    /// <para>
    /// If the owning object or collection did not exist on the right (e.g., the entire object or collection was removed), <see cref="RightOwner"/> will be <c>null</c>.
    /// </para>
    /// <para>
    /// <see cref="RightOwner"/> can be used with <see cref="DifferenceKind"/> and <see cref="RightValue"/> to inspect or apply changes on the updated object graph.
    /// </para>
    /// </remarks>
    [JsonIgnore]
    public object? RightOwner { get; set; }

    /// <summary>
    /// The value of the property or element on the left side (original).
    /// This will be <c>null</c> if the property or element was added in the right object graph.
    /// </summary>
    public object? LeftValue { get; set; }

    /// <summary>
    /// The value of the property or element on the right side (new/modified).
    /// This will be <c>null</c> if the property or element was removed from the left object graph.
    /// </summary>
    public object? RightValue { get; set; }

    /// <summary>
    /// Indicates the kind of difference represented by this object.
    /// </summary>
    /// <remarks>
    /// Use this property to quickly determine whether the difference is an addition (<see cref="DifferenceKind.Addition"/>),
    /// removal (<see cref="DifferenceKind.Removal"/>), or modification (<see cref="DifferenceKind.Modification"/>).
    /// This avoids having to infer the change type from <see cref="Path"/>, <see cref="LeftValue"/>, or <see cref="RightValue"/>.
    /// </remarks>
    public DifferenceKind Kind { get; set; }

    /// <summary>
    /// Returns a concise string representation of the difference.
    /// </summary>
    public override string ToString() => $"{Kind}: {Path}: {LeftValue} -> {RightValue}";
}
