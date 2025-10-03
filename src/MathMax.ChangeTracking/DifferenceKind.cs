namespace MathMax.ChangeTracking;

/// <summary>
/// Describes the type of change detected in a <see cref="Difference"/>.
/// </summary>
public enum DifferenceKind
{
    /// <summary>The property or element exists only on the right side (added).</summary>
    Addition,

    /// <summary>The property or element exists only on the left side (removed).</summary>
    Removal,

    /// <summary>The property or element exists on both sides but has a different value (modified).</summary>
    Modification
}
