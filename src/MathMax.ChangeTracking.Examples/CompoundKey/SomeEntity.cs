using System;
using System.Collections.Generic;

namespace MathMax.ChangeTracking.Examples.CompoundKey;

public class SomeEntity
{
    public Guid Id { get; set; }
    public string? Name { get; set; }
    public IReadOnlyList<SomeChildEntity> Children { get; set; } = [];
}
