using System;
using System.Collections.Generic;

namespace MathMax.ChangeTracking.Examples;

public class Person
{
    public Guid PersonId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public DateTime DateOfBirth { get; set; }
    public Address? Address { get; set; } = null;
    public IReadOnlyList<Order> Orders { get; set; } = [];
}
