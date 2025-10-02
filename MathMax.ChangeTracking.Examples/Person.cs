using System;
using System.Collections.Generic;

namespace MathMax.ChangeTracking.Examples;

public class Person
{
    public Guid PersonId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public DateTime DateOfBirth { get; set; }
    public Address? Addresses { get; set; } = new Address();
    public IReadOnlyList<Order> Orders { get; set; } = [];
}
