using System.Text.Json.Serialization;

namespace MathMax.ChangeTracking;

/// <summary>
/// Represents a single difference between two object graphs compared by the generated Diff extensions.
/// </summary>
public sealed class Difference
{
    public string Path { get; set; } = string.Empty;

    /// <summary>
    /// The object instance (left side) that owns the changed property indicated by <see cref="Path"/>.
    /// For nested differences this will be the nested object whose member changed; for collection / index
    /// differences this is the collection instance on the left side. Null if the owning object itself did
    /// not exist on the left side (e.g. entire object added).
    /// </summary>
    [JsonIgnore]
    public object? LeftOwner { get; set; }

    /// <summary>
    /// The object instance (right side) that owns the changed property indicated by <see cref="Path"/>.
    /// Null if the owning object itself did not exist on the right side (e.g. entire object removed).
    /// </summary>
    [JsonIgnore]
    public object? RightOwner { get; set; }

    public object? LeftValue { get; set; }

    public object? RightValue { get; set; }

    public override string ToString() => $"{Path}: {LeftValue} -> {RightValue}";
}
