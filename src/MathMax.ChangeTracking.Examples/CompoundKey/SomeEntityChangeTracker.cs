namespace MathMax.ChangeTracking.Examples.CompoundKey;

public static class SomeEntityChangeTracker
{
    static SomeEntityChangeTracker()
    {
        // Declare the map once. Static ctor ensures this appears in compilation for generator discovery.
        ChangeTracking.Map<SomeEntity>(p =>
        {
            p.Children.TrackBy(c => new { c.KeyPartA, c.KeyPartB, c.KeyPartC });
        });
    }
}
