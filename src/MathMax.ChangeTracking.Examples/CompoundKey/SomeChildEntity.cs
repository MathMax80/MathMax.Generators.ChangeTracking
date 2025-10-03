using System;

namespace MathMax.ChangeTracking.Examples.CompoundKey;

public class SomeChildEntity
{
    public Guid KeyPartA { get; set; }
    public string KeyPartB { get; set; } = null!;
    public int KeyPartC { get; set; }
    public string? Name { get; set; }
}
