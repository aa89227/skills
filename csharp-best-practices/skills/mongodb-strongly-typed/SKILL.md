---
name: mongodb-strongly-typed
description: MongoDB C# Driver strongly-typed patterns. Use when working with MongoDB collections, filters, updates, projections, aggregations, and indexes using strongly-typed expressions.
license: MIT
metadata:
  author: aa89227
  version: "2.0"
  tags: ["mongodb", "csharp", "database", "strongly-typed"]
---

# MongoDB Strongly-Typed Patterns

**Driver version:** MongoDB.Driver 3.x (.NET 8+)

## General Rules

- **Always prefer lambda expressions** over magic strings for type safety and refactoring support.
- Use `Builders<T>.Filter`, `Builders<T>.Update`, `Builders<T>.Sort`, `Builders<T>.Projection`.
- Use `IMongoCollection<T>.AsQueryable()` for LINQ queries (requires `MongoDB.Driver.Linq`).
- Use `Builders<T>.IndexKeys` for strongly-typed index definitions.
- For `arrayFilters` with `In`/`Nin` — use the `ArrayFilterIn` / `ArrayFilterNin` extension (see `references/array-filter-extension.md`).
- Convention registration must happen **once at startup** before any serialization.

```csharp
// ✅ Strongly-typed — refactor-safe, compile-time checked
var filter = Builders<User>.Filter.Eq(x => x.Email, "test@example.com");

// ❌ Magic string — runtime errors, no refactoring support
var filter = Builders<User>.Filter.Eq("Email", "test@example.com");
```

## Cheat Sheet

| Topic | Strongly-Typed | Avoid (Magic String) |
|---|---|---|
| **Filter (Equality)** | `Filter.Eq(x => x.Id, id)` | `Filter.Eq("_id", id)` |
| **Filter (Comparison)** | `Filter.Gte(x => x.Age, 18)` | `Filter.Gte("Age", 18)` |
| **Filter (Array)** | `Filter.AnyEq(x => x.Tags, tag)` | `Filter.AnyEq("Tags", tag)` |
| **Filter (In)** | `Filter.In(x => x.Tags, arr)` | `Filter.In("Tags", arr)` |
| **Filter (Nin)** | `Filter.Nin(x => x.Tags, arr)` | `Filter.Nin("Tags", arr)` |
| **Filter (All)** | `Filter.All(x => x.Tags, arr)` | `Filter.All("Tags", arr)` |
| **Update (Set)** | `Update.Set(x => x.Name, name)` | `Update.Set("Name", name)` |
| **Update (Inc)** | `Update.Inc(x => x.Age, 1)` | `Update.Inc("Age", 1)` |
| **Update (Array)** | `Update.AddToSet(x => x.Tags, tag)` | `Update.AddToSet("Tags", tag)` |
| **Update ($)** | `x.Items.FirstMatchingElement().Field` | `"Items.$.Field"` |
| **Update ($[])** | `x.Items.AllElements().Field` | `"Items.$[].Field"` |
| **Update ($[id])** | `x.Items.AllMatchingElements("id").Field` | `"Items.$[id].Field"` |
| **ArrayFilter (In)** | `ArrayFilterIn(x => x.Items, i => i.Status, arr, "id")` | `new BsonDocument(...)` |
| **ArrayFilter (Nin)** | `ArrayFilterNin(x => x.Items, i => i.Status, arr, "id")` | `new BsonDocument(...)` |
| **Sort (Asc)** | `Sort.Ascending(x => x.CreatedAt)` | `Sort.Ascending("CreatedAt")` |
| **Sort (Desc)** | `Sort.Descending(x => x.Age)` | `Sort.Descending("Age")` |
| **Project** | `Projection.Expression(x => new { x.Name })` | `Projection.Include("Name")` |
| **Index (Asc)** | `IndexKeys.Ascending(x => x.Email)` | `IndexKeys.Ascending("Email")` |
| **Index (Text)** | `IndexKeys.Text(x => x.Name)` | `IndexKeys.Text("Name")` |
| **Lookup** | `Lookup(..., u => u.Id, o => o.UserId, ...)` | `Lookup("col", "_id", "UserId", ...)` |
| **Unwind** | `Unwind<T, TResult>(x => x.Array)` | `Unwind("Array")` |
| **Aggregation** | `Aggregate().Match(x => x.IsActive)` | `Aggregate().Match("{ IsActive: true }")` |

## Best Practices

1. **Always use lambda expressions** — type safety, IDE IntelliSense, refactoring support.
2. **Use `AsQueryable()` for simple LINQ** — translates directly to MongoDB query language.
3. **Use aggregation pipeline for complex operations** — Lookup, Facet, Group, Unwind.
4. **Create indexes with `Builders<T>.IndexKeys`** — compile-time field validation.
5. **Use `BulkWriteAsync`** for batch operations — reduces round trips.
6. **Leverage projection** — only select fields you need to reduce network payload.
7. **Chain update operators** — `Builders<T>.Update.Set().Set().Inc()` for atomic updates.
8. **For `arrayFilters` with `In`/`Nin`** — use `ArrayFilterIn`/`ArrayFilterNin` extensions, not raw `BsonDocument`.

## Additional Resources

### Example Files

Complete, runnable `.cs` examples in `examples/`:
- **`examples/strongly-typed-repository.cs`** — Full `UserRepository` with Filter, Update, positional operators (`$`, `$[]`, `$[id]`), arrayFilters, Aggregation (Facet, Unwind, Lookup), BulkWrite, Index creation
- **`examples/conventions-serializers.cs`** — CamelCase convention, CamelCase enum serializer, strongly-typed ID (`UserId` record + `UserIdBsonSerializer`)

### Reference Files

- **`references/array-filter-extension.md`** — `ArrayFilterIn`/`ArrayFilterNin` full implementation + usage example
