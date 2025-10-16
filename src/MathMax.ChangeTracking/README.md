# MathMax.ChangeTracking

[![NuGet](https://img.shields.io/nuget/v/MathMax.ChangeTracking.svg)](https://www.nuget.org/packages/MathMax.ChangeTracking/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

**Runtime library for efficient, allocation‑friendly object graph diffing.**

This package provides the core types, fluent DSL, and difference handling infrastructure used by the [MathMax.Generators.ChangeTracking](https://github.com/MathMax80/MathMax.Generators.ChangeTracking) source generator. While designed to work together, this runtime library can be used independently for scenarios where you want to manually implement diffing logic or integrate with other code generation approaches.

> 📖 **For complete documentation, examples, and usage patterns**, see the main [MathMax.Generators.ChangeTracking repository](https://github.com/MathMax80/MathMax.Generators.ChangeTracking).

## What This Package Provides

This runtime library contains:

- **Core Types**: `Difference`, `DifferenceKind`, and related models for representing changes
- **DSL Markers**: `ChangeTracking.Map<T>()` and `TrackBy()` extensions for declaring tracking configuration  
- **Difference Handling**: `IDifferenceHandler<TModel, TEntity>` and `DifferenceDispatcher<TModel, TEntity>` for processing changes
- **Extensibility**: Interfaces and base types for custom difference processing

## Installation

**With Source Generator** (Recommended):
```xml
<PackageReference Include="MathMax.Generators.ChangeTracking" Version="1.0.0" />
<PackageReference Include="MathMax.ChangeTracking" Version="1.0.0" />
```

**Runtime Only** (for custom implementations):
```xml
<PackageReference Include="MathMax.ChangeTracking" Version="1.0.0" />
```

## Quick Start

> 💡 **Using with the Source Generator?** See the [complete quick start guide](https://github.com/MathMax80/MathMax.Generators.ChangeTracking#quick-start) in the main repository.

### Basic Usage Example

```csharp
using MathMax.ChangeTracking;

// 1. Declare tracking configuration (when using with source generator)
public static class PersonChangeTracker
{
    static PersonChangeTracker()
    {
        ChangeTracking.Map<Person>(p =>
        {
            p.Orders.TrackBy(o => o.OrderId);
        });
    }
}

// 2. Work with differences (manual or generated)
IEnumerable<Difference> diffs = GetDifferencesSomehow(original, modified);

foreach (var diff in diffs)
{
    Console.WriteLine($"{diff.Kind}: {diff.Path}: {diff.LeftValue} -> {diff.RightValue}");
}
```

## Core Types

### Difference

Represents a single change between two object graphs:

```csharp
public sealed class Difference
{
    public string Path { get; set; }           // e.g., "Person.Orders[123].Items[456].Quantity"
    public object? LeftOwner { get; set; }     // Owner object on left side
    public object? RightOwner { get; set; }    // Owner object on right side  
    public object? LeftValue { get; set; }     // Original value
    public object? RightValue { get; set; }    // New value
    public DifferenceKind Kind { get; set; }   // Addition, Removal, or Modification
}
```

### DifferenceKind

```csharp
public enum DifferenceKind
{
    Addition,     // Element/property exists only on the right side
    Removal,      // Element/property exists only on the left side  
    Modification  // Element/property exists on both sides but differs
}
```

## Path Format

Paths provide precise location information for changes:

- **Scalar property**: `Person.FirstName`
- **Nested property**: `Person.Address.Street`
- **Collection element**: `Person.Orders[123]` (tracked by OrderId)
- **Element property**: `Person.Orders[123].Items[456].Quantity`
- **Composite keys**: `Person.Items[OrderId_ProductId]` (anonymous type toString)

## Change Tracking DSL

> 📖 **For detailed DSL documentation and examples**, see the [main repository documentation](https://github.com/MathMax80/MathMax.Generators.ChangeTracking#dsl-explained).

The DSL consists of compile-time markers that the source generator inspects:

- **`ChangeTracking.Map<T>()`** - Defines root entity mapping
- **`TrackBy()`** - Declares identity-based collection tracking with optional nesting

```csharp
ChangeTracking.Map<Person>(p =>
{
    p.Orders.TrackBy(o => o.OrderId, o =>
    {
        o.Items.TrackBy(i => new { i.OrderId, i.ProductId });
    });
});
```

## Difference Handling

### Built-in Dispatcher

Use the `DifferenceDispatcher` to route differences to specific handlers:

```csharp
public class PersonNameHandler : IDifferenceHandler<Person, Person>
{
    public Regex PathPattern { get; } = new("^Person\\.(FirstName|LastName)$");
    
    public void Handle(Difference diff, Match match, Person original, Person altered, Person entity)
    {
        Console.WriteLine($"Name changed: {diff.LeftValue} -> {diff.RightValue}");
        // Apply business logic, emit events, update indexes, etc.
    }
}

var handlers = new IDifferenceHandler<Person, Person>[] 
{
    new PersonNameHandler()
};

var dispatcher = new DifferenceDispatcher<Person, Person>(handlers);
var result = dispatcher.Dispatch(diffs, original, modified, modified);

Console.WriteLine($"Handled: {result.Handled.Length}, Unhandled: {result.Unhandled.Length}");
```

### Custom Handlers

Implement `IDifferenceHandler<TModel, TEntity>`:

```csharp
public interface IDifferenceHandler<TModel, TEntity>
{
    Regex PathPattern { get; }
    void Handle(Difference difference, Match match, TModel original, TModel altered, TEntity entity);
}
```

## Usage Scenarios

### With Source Generator
The typical use case - declare mappings using the DSL and let the generator create strongly-typed `GetDifferences()` extension methods.

### Manual Implementation  
Use the core types and interfaces to build custom diffing logic:

```csharp
public static class CustomDiffer
{
    public static IEnumerable<Difference> GetPersonDifferences(Person left, Person right)
    {
        if (left.FirstName != right.FirstName)
        {
            yield return new Difference
            {
                Path = "Person.FirstName",
                LeftValue = left.FirstName,
                RightValue = right.FirstName,
                Kind = DifferenceKind.Modification,
                LeftOwner = left,
                RightOwner = right
            };
        }
        // ... additional property comparisons
    }
}
```

### Integration with Other Generators
The types in this package can be used as a foundation for other code generation approaches or reflection-based solutions.

## Integration Examples

### Entity Framework Change Tracking

```csharp
public class OrderAuditHandler : IDifferenceHandler<Order, Order>
{
    private readonly DbContext _context;
    public Regex PathPattern { get; } = new("^Order\\.");

    public void Handle(Difference diff, Match match, Order original, Order altered, Order entity)
    {
        _context.AuditLogs.Add(new AuditLog
        {
            EntityId = entity.OrderId,
            PropertyPath = diff.Path,
            OldValue = diff.LeftValue?.ToString(),
            NewValue = diff.RightValue?.ToString(),
            ChangeType = diff.Kind.ToString(),
            Timestamp = DateTime.UtcNow
        });
    }
}
```

### Event Sourcing

```csharp
public class PersonEventHandler : IDifferenceHandler<Person, Person>
{
    private readonly IEventStore _eventStore;
    public Regex PathPattern { get; } = new("^Person\\.");

    public void Handle(Difference diff, Match match, Person original, Person altered, Person entity)
    {
        var @event = diff.Kind switch
        {
            DifferenceKind.Modification when match.Groups[1].Value == "FirstName" 
                => new PersonNameChanged(entity.PersonId, diff.LeftValue?.ToString(), diff.RightValue?.ToString()),
            _ => null
        };

        if (@event != null)
            _eventStore.SaveEvent(@event);
    }
}
```

## Repository Structure

This package is part of the [MathMax.Generators.ChangeTracking](https://github.com/MathMax80/MathMax.Generators.ChangeTracking) repository:

- **`MathMax.Generators.ChangeTracking`** - Source generator package
- **`MathMax.ChangeTracking`** - This runtime library  
- **`MathMax.ChangeTracking.Examples`** - Sample implementations and usage patterns

## Documentation & Examples

- 📖 **[Complete Documentation](https://github.com/MathMax80/MathMax.Generators.ChangeTracking)** - Full usage guide, examples, and API reference
- 🧪 **[Examples Project](https://github.com/MathMax80/MathMax.Generators.ChangeTracking/tree/main/src/MathMax.ChangeTracking.Examples)** - Sample entities and change tracker implementations
- 🎯 **[Quick Start Guide](https://github.com/MathMax80/MathMax.Generators.ChangeTracking#quick-start)** - Get up and running in minutes

## Contributing

Contributions are welcome! Please see the [main repository](https://github.com/MathMax80/MathMax.Generators.ChangeTracking) for:

- Issue reporting and feature requests
- Development guidelines and contribution process
- Full test suite and examples

## License

This project is licensed under the MIT License - see the [LICENSE](https://github.com/MathMax80/MathMax.Generators.ChangeTracking/blob/main/LICENSE) file for details.