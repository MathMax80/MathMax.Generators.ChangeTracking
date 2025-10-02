# MathMax.Generators.ChangeTracking

Lightweight C# source generator that produces efficient, allocation‑friendly diffing code for your aggregate / entity graphs. You declare **what collections are keyed** (identity) using a tiny fluent DSL (`ChangeTracking.Map` + `TrackBy`). The generator emits strongly‑typed `GetDifferences` extension methods that walk two equal object hierarchies (original + modified) and yield a normalized stream of `Difference` objects.

> Zero reflection at runtime. No expression tree compilation. Pure compile‑time generation + straightforward POCO iteration.

## Why?
Typical change tracking for nested aggregates (Orders → Items, Person → Addresses, etc.) either:
* Re‑implements ad‑hoc recursive comparison logic per model.
* Uses heavy reflection / JSON diff libs with extra boxing & allocations.
* Fails to treat collections by identity (so reorder = massive diff noise).

This project gives you:
* **Deterministic paths** (`Person.Addresses[12345].Street`) enabling filtering / dispatch.
* **Identity-aware collection diffing** via `TrackBy(x => x.Key)` – stable regardless of ordering.
* **Recursive scalar + complex property comparison** with early null short‑circuit.
* **Composable post-processing** (regex‑based `IDifferenceHandler` & dispatcher included).
* **Simple primitives detection** (no accidental deep walk of value objects like `Guid`).

## Packages / Projects
| Project | Purpose | Target |
|--------|---------|--------|
| `MathMax.Generators.ChangeTracking` | Source generator (adds `*.ChangeTracking.g.cs`) | netstandard2.0 (Roslyn) |
| `MathMax.ChangeTracking` | Runtime helper types (`Difference`, dispatcher, DSL markers) | net9.0 |
| `MathMax.ChangeTracking.Examples` | Example POCOs + hand-crafted sample | net9.0 |

> NuGet packaging instructions TBD (add badges once published).

## Quick Start
1. Reference the generator + runtime library projects (or future NuGet packages) from your domain project.
2. In any `.cs` file, declare a mapping describing which collections are keyed:

```csharp
using MathMax.ChangeTracking;
using Your.Domain.Models;

public static class ChangeTrackingConfig
{
	static ChangeTrackingConfig()
	{
		ChangeTracking.Map<Person>(p =>
		{
			p.Addresses.TrackBy(a => new { a.ZipCode, a.HouseNumber }); // composite key
			p.Orders.TrackBy(o => o.OrderId, o =>
			{
				// Nested keyed collection inside Order
				o.Items.TrackBy(i => i.ProductId);
			});
		});
	}
}
```

3. Build the solution. A generated file (e.g. `Person.ChangeTracking.g.cs`) appears in Analyzers → Generated Files.
4. Use the generated extension to diff two graphs:

```csharp
IEnumerable<Difference> diffs = modifiedPerson.GetDifferences(originalPerson);
foreach (var d in diffs)
	Console.WriteLine(d); // Person.FirstName: John -> Jonathan
```

## DSL Explained
`ChangeTracking.Map<TRoot>(lambda)` is a **compile‑time marker**. The lambda executes at runtime with `default!` so avoid dereferencing – only call `TrackBy` on collection navigation properties.

`collection.TrackBy(x => x.Key, x => { /* nested TrackBy for child collections */ })`
* First lambda chooses the identity key (can be an anonymous type for composite keys).
* Optional second lambda describes nested keyed collections of the element type.

No runtime data structures are stored – the generator inspects the syntax tree.

## Generated Code Shape
For a root entity `Person` a static internal class `PersonChangeTrackerGenerated` is produced with:
* `public static IEnumerable<Difference> GetDifferences(this Person right, Person left, string path = nameof(Person))`
* Private overloads for each involved complex type encountered.
* Inline scalar comparisons: `if (left.FirstName != right.FirstName) yield return ...`
* Collection diff loops invoking `DiffListByIdentity` with your key selector expression text.

### Path Format
* Scalar property: `Person.FirstName`
* Complex nested: `Person.Address.City`
* Collection element: `Person.Addresses[ZIP_HOUSE]` (where `ZIP_HOUSE` is the projected key – for anonymous composite keys the `ToString()` of the anonymous instance is used).
* Property of a collection element: `Person.Orders[42].Items[5001].Quantity`

## Difference Model
`Difference` contains:
* `Path` – unique descriptor usable for matching/regex.
* `LeftOwner` / `RightOwner` – owning object instance (null if object added/removed entirely).
* `LeftValue` / `RightValue` – the property / presence values.

### Presence vs Property Differences
* Presence (added/removed): `LeftValue` or `RightValue` will be null and `Path` points to collection slot (`Orders[123]`).
* Property: `Path` includes `.PropertyName`.

## Dispatching Differences
Implement `IDifferenceHandler<TModel,TEntity>` with a `Regex PathPattern` to react to selected changes (e.g., update denormalized read model, emit events):

```csharp
public class PersonNameChangedHandler : IDifferenceHandler<Person, Person>
{
	public Regex PathPattern { get; } = new("^Person\\.(FirstName|LastName)$", RegexOptions.Compiled);
	public void Handle(Difference diff, Match match, Person original, Person altered, Person entity)
	{
		// e.g. mark entity as needing re-indexing
	}
}

var dispatcher = new DifferenceDispatcher<Person, Person>(new IDifferenceHandler<Person, Person>[] { new PersonNameChangedHandler() });
var result = dispatcher.Dispatch(diffs, originalPerson, modifiedPerson, modifiedPerson);
Console.WriteLine($"Handled: {result.Handled.Length}, Unhandled: {result.Unhandled.Length}");
```

## Performance Notes
* Single pass over each collection (no sorting or double enumeration).
* Keys resolved once; dictionary lookups O(1) for presence.
* Only allocs: `Difference` objects + ephemeral enumerators.
* No reflection per element; generator emits strongly typed property access.

## Guidelines & Gotchas
* Execute `Map` configuration once at startup (static ctor pattern); duplicate maps are de‑duplicated by generation grouping.
* Do not rely on execution order inside the lambda – only the presence of `TrackBy` calls matters.
* Anonymous composite keys should be stable (avoid floating-point parts prone to precision changes).
* Nullable reference scalar comparisons rely on standard `!=` semantics; override equality for custom structs if needed.
* Currently only one `TrackBy` per distinct `(OwnerType, CollectionProperty)` in output (duplicates collapsed).

## Roadmap / Ideas
* [ ] NuGet packaging & version badge.
* [ ] Optionally generate diff direction `left vs right` parameter order consistency (currently `right, left`).
* [ ] Configurable path escaping for keys containing `]` or `.`.
* [ ] Pluggable value comparers (e.g., case-insensitive strings, tolerance for decimals).
* [ ] Generation of specialized handlers or strongly‑typed events.
* [ ] Benchmarks project (BenchmarkDotNet) documenting throughput & allocation vs reflection libs.

## Contributing
PRs and issues welcome. Please:
1. Describe the scenario / failing case clearly.
2. Add or update unit tests (tests project TBD).
3. Keep generator output deterministic (avoid DateTime.Now, GUIDs, etc.).

## License
MIT (see `LICENSE`).

## Minimal Example (Inline)
```csharp
var original = new Person { FirstName = "John", LastName = "Smith", PersonId = Guid.NewGuid() };
var modified = new Person { FirstName = "Jonathan", LastName = "Smith", PersonId = original.PersonId };

ChangeTracking.Map<Person>(p => { /* even empty map will still compare scalars */ });
var diffs = modified.GetDifferences(original); // generated after build
// Yields: Person.FirstName difference
```

---
Feel free to open an issue if a desired scenario (e.g., dictionaries, value object custom equality, ignoring properties) is missing.
