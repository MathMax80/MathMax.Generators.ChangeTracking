using System;
using System.Collections.Generic;

namespace MathMax.ChangeTracking.Examples;

public class Order
{
    public int OrderId { get; set; }
    public DateTime OrderDate { get; set; }
    public IReadOnlyList<OrderItem> Items { get; set; } = [];
}
