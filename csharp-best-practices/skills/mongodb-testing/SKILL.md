---
name: mongodb-testing
description: |
  MongoDB integration testing patterns with Testcontainers and strongly-typed operations.
  Use when writing or reviewing MongoDB integration tests that use Testcontainers, strongly-typed
  Builders<T> for test data preparation, and Verify snapshot testing for BSON serialization shapes.
  Complements the mongodb-strongly-typed skill with testing-specific patterns.
  Trigger phrases: "MongoDB test", "Testcontainers MongoDB", "BSON test", "serialization test",
  "MongoDB integration test", "snapshot BSON", "test data MongoDB".
license: MIT
metadata:
  author: aa89227
  version: "1.0"
  tags: ["testing", "mongodb", "testcontainers", "bson", "integration-test", "verify"]
---

# MongoDB Integration Testing

**Frameworks:** NUnit 4.x, Testcontainers.MongoDb, MongoDB.Driver 3.x, Verify.NUnit

Complements the **`mongodb-strongly-typed`** skill — this skill covers the **testing** side.

## General Rules

- Use **Testcontainers** for real MongoDB instances — no in-memory fakes or mocks.
- **Singleton container** shared across all tests — avoid per-test container startup overhead.
- **Drop database before each test** — full isolation without container restart.
- All MongoDB operations in tests must use **strongly-typed expressions** (see `mongodb-strongly-typed` skill).
- Use **Verify snapshots** for BSON serialization shape testing — catch unintended schema changes.

## Testcontainers Setup

### Container Configuration

```csharp
// In TestServer / WebApplicationFactory
private MongoDbContainer? _mongoDbContainer;

private void InitialMongoDb()
{
    if (_mongoDbContainer != null) return;
    _mongoDbContainer = new MongoDbBuilder()
        .WithImage("mongo:8.0")
        .WithReplicaSet()  // Required for transactions / change streams
        .Build();
    _mongoDbContainer.StartAsync().Wait();
}
```

Key points:
- **`mongo:8.0`** image — pin major version for reproducibility.
- **`.WithReplicaSet()`** — enables transactions and change streams support.
- **Lazy initialization** — container only starts when first test runs.

### Singleton Pattern

```csharp
// Container lives for the entire test run
private static readonly Lazy<TestServer> Instance = new(() => new TestServer(...));

[SetUp]
public async Task BeforeEach()
{
    await Server.DropDatabaseAsync();  // Clean state per test
    Server.ResetIdGenerator();
}
```

- `Lazy<T>` ensures one container instance for all tests.
- `DropDatabaseAsync()` is fast enough for per-test isolation (faster than container restart).
- No `[TearDown]` or `DisposeAsync` needed — container lives for the process lifetime.

### Connection String Override

```csharp
builder.ConfigureHostConfiguration(config =>
{
    var settings = new Dictionary<string, string>
    {
        [$"ConnectionStrings:{DbSectionName}"] = _mongoDbContainer.GetConnectionString(),
        [$"{DbSectionName}:Database"] = "TestDatabase",
    };
    config.AddInMemoryCollection(settings!);
});
```

- Override connection strings via `AddInMemoryCollection` — no test-specific config files.
- Use a fixed database name (`"TestDatabase"`) for simplicity.
- Multiple DbContexts can share the same container with different database names.

## Test Data Operations

### Accessing Collections via DI

```csharp
// Get DbContext from DI container
var dbContext = server.GetRequiredService<XxxDbContext>();
var collection = dbContext.GetCollection<XxxDto>();
```

- Always resolve from **DI container** via `TestServer.GetRequiredService<T>()`.
- Use `GetCollection<TDto>()` for strongly-typed collection access.

### Data Preparation (Given)

```csharp
// ✅ Strongly-typed — refactor-safe
var filter = Builders<XxxDto>.Filter.Eq(x => x.Id, id);
var update = Builders<XxxDto>.Update.Set(x => x.Name, name);
await collection.UpdateOneAsync(filter, update);

// ✅ Lambda filter shorthand
await collection.UpdateOneAsync(x => x.Id == id, update);

// ✅ AddToSet for array fields
var update = Builders<XxxDto>.Update.AddToSet(x => x.RelatedIds, relatedId);

// ❌ Magic string — runtime errors, no refactoring support
var filter = Builders<XxxDto>.Filter.Eq("_id", id);
var update = Builders<XxxDto>.Update.Set("Name", name);
```

Rules:
- **Always use lambda expressions** for Filter, Update, Sort, Projection.
- **Never use magic strings** — see `mongodb-strongly-typed` cheat sheet for all operators.
- Prefer **API calls** for data setup; use direct MongoDB operations only when API doesn't support the operation.

### Data Insertion

```csharp
// Insert a typed DTO
await collection.InsertOneAsync(new XxxDto
{
    Id = "test-id",
    Name = "test-name",
    // ... all required fields
});
```

- Prefer typed DTOs over raw `BsonDocument` for insert operations.
- Set all required fields explicitly — don't rely on defaults in tests.

## BSON Serialization Shape Testing

Use Verify snapshots to guard against unintended changes to BSON serialization output.

### Sample Factory Pattern

```csharp
internal static class SampleFactory
{
    // Shared deterministic test values
    private static readonly string TestId = "507f1f77bcf86cd799439011";
    private static readonly DateTime TestDate = new(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    public static Dictionary<Type, TBase> CreateAll()
    {
        return new Dictionary<Type, TBase>
        {
            [typeof(ConcreteTypeA)] = new ConcreteTypeA { /* fully populated */ },
            [typeof(ConcreteTypeB)] = new ConcreteTypeB { /* fully populated */ },
        };
    }
}
```

Rules:
- Use **static deterministic values** — no `Guid.NewGuid()`, no `DateTime.Now`.
- **Fully populate** all fields — including explicitly setting nullable fields to `null`.
- **One entry per concrete type** — keyed by `typeof(T)`.

### Coverage Guard Test

```csharp
[Test]
public void AllImplementationsHaveCorrespondingSamples()
{
    var allTypes = typeof(TBase).Assembly.GetTypes()
        .Where(t => t is { IsAbstract: false, IsInterface: false })
        .Where(t => typeof(TBase).IsAssignableFrom(t))
        .ToHashSet();

    var sampleTypes = SampleFactory.CreateAll().Keys.ToHashSet();

    Assert.That(sampleTypes, Is.EquivalentTo(allTypes));
}
```

- **Reflection-based** — automatically fails when a new concrete type is added without a sample.
- Ensures 100% coverage of the sample factory.

### Snapshot Shape Test

```csharp
[Test]
public async Task XxxAggregateSerializationShape()
{
    var items = SampleFactory.CreateAll()
        .Where(kv => kv.Key.Namespace!.Contains("XxxAggregate"))
        .Select(kv => kv.Value)
        .OrderBy(e => e.GetType().Name)
        .Select(e => e.ToBsonDocument(typeof(TBase)).ToString())
        .ToList();

    await Verifier.Verify(items);
}
```

Pattern:
1. Filter samples by namespace / category.
2. Order by type name for deterministic output.
3. Serialize to BSON via `ToBsonDocument(typeof(TBase))` — uses the registered polymorphic serializer.
4. Convert to string `.ToString()` for human-readable snapshot.
5. `Verifier.Verify()` compares against `.verified.txt` snapshot file.

## Cheat Sheet

| Topic | Pattern |
|---|---|
| **Container** | `new MongoDbBuilder().WithImage("mongo:8.0").WithReplicaSet().Build()` |
| **Isolation** | `DropDatabaseAsync()` in `[SetUp]` |
| **Get collection** | `server.GetRequiredService<DbContext>().GetCollection<TDto>()` |
| **Filter** | `Builders<T>.Filter.Eq(x => x.Id, id)` |
| **Update** | `Builders<T>.Update.Set(x => x.Name, name)` |
| **Insert** | `collection.InsertOneAsync(new TDto { ... })` |
| **BSON snapshot** | `obj.ToBsonDocument(typeof(TBase)).ToString()` → `Verifier.Verify()` |
| **Coverage guard** | Reflection to assert all types have samples |
| **Connection override** | `AddInMemoryCollection` with container `GetConnectionString()` |

## Best Practices

1. **Singleton container** — one per test run, `Lazy<T>`, never per-test.
2. **Drop database, not container** — `DropDatabaseAsync()` is faster than container restart.
3. **Pin MongoDB image version** — avoid surprise failures from image updates.
4. **Enable ReplicaSet** — even if not using transactions now, prevents future migration pain.
5. **Strongly-typed everything** — no magic strings in filters, updates, or projections.
6. **Typed DTOs over BsonDocument** — for insert and query operations in tests.
7. **Deterministic test data** — fixed IDs, fixed dates, no random values.
8. **Sample factory coverage guard** — reflection test to catch missing samples automatically.
9. **Namespace-grouped snapshots** — one snapshot test per aggregate/module for manageable diffs.

## Additional Resources

### Reference Files

- **`references/reviewer-checklist.md`** — Reviewer checklist for verifying MongoDB test compliance
