namespace MathMax.ChangeTracking.Examples.Trackers;

public static class PersonChangeTracker
{
    static PersonChangeTracker()
    {
        // Declare the map once. Static ctor ensures this appears in compilation for generator discovery.
        ChangeTracking.Map<Person>(p =>
        {
            p.Orders.TrackBy(o => o.OrderId, o =>
            {
                o.Items.TrackBy(i => new { i.OrderId, i.ProductId });
            });
        });
    }
}
