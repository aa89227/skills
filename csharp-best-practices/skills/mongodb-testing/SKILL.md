---
name: mongodb-testing
description: |
  MongoDB integration testing patterns with Testcontainers and strongly-typed operations.
  Use when writing or reviewing MongoDB integration tests that use Testcontainers, strongly-typed
  Builders<T> for test data preparation, and Verify snapshot testing for BSON serialization shapes.
  Includes Atlas Local container for Atlas Search / Vector Search testing, search index definition
  builder, index lifecycle management, and eventual consistency wait patterns.
  Complements the mongodb-strongly-typed skill with testing-specific patterns.
  Trigger phrases: "MongoDB test", "Testcontainers MongoDB", "BSON test", "serialization test",
  "MongoDB integration test", "snapshot BSON", "test data MongoDB", "Atlas Local test",
  "Atlas Search test", "SearchTestBase", "atlas-local testcontainer", "search index test",
  "SearchIndexDefinitionBuilder", "AtlasSearchIndexManager", "wait for search index".
license: MIT
metadata:
  author: aa89227
  version: "2.0"
  tags: ["testing", "mongodb", "testcontainers", "bson", "integration-test", "verify", "atlas-local", "atlas-search"]
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

### Atlas Local Container (Atlas Search / Vector Search)

When tests require `$search` or `$vectorSearch`, use `mongodb/mongodb-atlas-local` instead of `mongo:8.0`:

```csharp
public sealed class MongoDbContainerManager : IAsyncDisposable
{
    private readonly MongoDbContainer _container;
    private readonly string _connectionString;

    public MongoDbContainerManager()
    {
        _container = new MongoDbBuilder()
            .WithImage("mongodb/mongodb-atlas-local:8.2.4")
            .WithUsername(null)   // Atlas Local — no auth required
            .WithPassword(null)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilContainerIsHealthy())
            .Build();

        _container.StartAsync().Wait();
        _connectionString = _container.GetConnectionString();
    }

    public string GetConnectionString() => _connectionString;

    public async Task DropDatabaseAsync(string databaseName)
    {
        var settings = MongoClientSettings.FromConnectionString(_connectionString);
        var client = new MongoClient(settings);
        await client.DropDatabaseAsync(databaseName);
    }

    public async ValueTask DisposeAsync() => await _container.DisposeAsync();
}
```

Key differences from `mongo:8.0`:
- **No auth** — `.WithUsername(null).WithPassword(null)`.
- **Health check wait** — `.WithWaitStrategy(Wait.ForUnixContainer().UntilContainerIsHealthy())` instead of default.
- **Atlas Search / Vector Search** — supports `$search` and `$vectorSearch` pipeline stages.
- **No `.WithReplicaSet()`** — Atlas Local automatically runs as a replica set.

**When to use which:**

| Feature needed | Image |
|---|---|
| CRUD, transactions, change streams | `mongo:8.0` + `.WithReplicaSet()` |
| `$search`, `$vectorSearch`, `$rankFusion` | `mongodb/mongodb-atlas-local:8.2.4` |

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

### SearchTestBase Pattern (Atlas Search)

When testing Atlas Search, the lifecycle is more complex — search indexes are expensive to create and eventually consistent:

```csharp
public abstract class SearchTestBase
{
    private static readonly Lazy<Task<MongoDbContainerManager>> ContainerInit =
        new(InitializeContainerAsync);

    protected static MongoDbContainerManager Container =>
        ContainerInit.Value.GetAwaiter().GetResult();

    private static async Task<MongoDbContainerManager> InitializeContainerAsync()
    {
        var container = new MongoDbContainerManager();

        // Create search indexes once for the entire test run
        await CreateSearchIndexesAsync(container.GetConnectionString());

        // Wait until indexes are READY
        await WaitForSearchIndexReadyAsync(collection);

        return container;
    }

    [SetUp]
    public virtual async Task SetUp()
    {
        // Per-test: clear data, NOT drop database (preserves search indexes)
        await ClearTestDataAsync();
    }
}
```

Key pattern:
1. **`Lazy<Task<T>>`** — async singleton; container + index creation runs exactly once.
2. **Create indexes once** — `SearchIndexes.CreateOneAsync()` after container starts.
3. **Wait for READY** — poll index status before any test runs.
4. **Per-test cleanup** — `DeleteManyAsync` + wait for index removal (**not** `DropDatabaseAsync`).

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

## Atlas Search Test Patterns

Atlas Search is **eventually consistent** — after writes, the search index updates asynchronously. Tests must poll until the index reflects the expected state.

### Wait for Index READY

After creating a search index, poll until its status becomes `"READY"`:

```csharp
private static async Task WaitForSearchIndexReadyAsync(
    IMongoCollection<TDocument> collection, string indexName)
{
    var timeout = TimeSpan.FromMinutes(2);
    var sw = Stopwatch.StartNew();

    while (sw.Elapsed < timeout)
    {
        var indexes = await collection.SearchIndexes.ListAsync();
        var indexList = await indexes.ToListAsync();
        var searchIndex = indexList.FirstOrDefault(x => x["name"].AsString == indexName);

        if (searchIndex is not null)
        {
            var status = searchIndex.GetValue("status", "UNKNOWN").AsString;

            if (status == "READY") return;
            if (status == "FAILED")
                throw new InvalidOperationException($"Search index creation failed: {searchIndex}");
        }

        await Task.Delay(TimeSpan.FromSeconds(5));
    }

    throw new TimeoutException($"Search index not READY within {timeout.TotalSeconds}s");
}
```

- **Timeout**: 2 minutes (index creation is slow on first run).
- **Poll interval**: 5 seconds.
- **Throw on FAILED** — don't silently wait forever.

### Wait for Data Indexing

After inserting documents, poll `$search` until the index reflects the new data:

```csharp
private async Task WaitForSearchIndexing(
    IMongoCollection<TDocument> collection, string[] ids, string indexName)
{
    var timeout = TimeSpan.FromSeconds(30);
    var sw = Stopwatch.StartNew();
    var searchDef = Builders<TDocument>.Search.In(x => x.Id, ids);

    while (sw.Elapsed < timeout)
    {
        var count = collection.Aggregate()
            .Search(searchDef, new SearchOptions<TDocument> { IndexName = indexName })
            .Count()
            .FirstOrDefault()?.Count ?? 0;

        if (count >= ids.Length) return;

        await Task.Delay(500);
    }

    throw new TimeoutException($"Search indexing not complete within {timeout.TotalSeconds}s");
}
```

- **Timeout**: 30 seconds (data indexing is faster than index creation).
- **Poll interval**: 500ms.
- Must specify **`SearchOptions<T> { IndexName = "..." }`** — default index name doesn't work.

### Wait for Index Removal

After deleting documents for test cleanup, poll until the index no longer returns them:

```csharp
private async Task WaitForSearchIndexRemoval(
    IMongoCollection<TDocument> collection, string[] ids, string indexName)
{
    var timeout = TimeSpan.FromSeconds(30);
    var sw = Stopwatch.StartNew();
    var searchDef = Builders<TDocument>.Search.In(x => x.Id, ids);

    while (sw.Elapsed < timeout)
    {
        var count = collection.Aggregate()
            .Search(searchDef, new SearchOptions<TDocument> { IndexName = indexName })
            .Count()
            .FirstOrDefault()?.Count ?? 0;

        if (count == 0) return;

        await Task.Delay(500);
    }

    throw new TimeoutException($"Search index removal not complete within {timeout.TotalSeconds}s");
}
```

### Wait for Nested Document Fields

Atlas Search indexes nested document fields **independently**. `documents.id` being indexed does NOT mean `documents.year` is also indexed. Verify with actual query filters:

```csharp
private async Task WaitForNestedFieldsIndexedAsync(
    IMongoCollection<TDocument> collection, TDocument item, string indexName)
{
    var searchBuilder = Builders<TDocument>.Search;
    var nestedBuilder = Builders<NestedDoc>.Search;
    var timeout = TimeSpan.FromSeconds(30);
    var sw = Stopwatch.StartNew();

    // Extract distinct (field1, field2) pairs from inserted data
    var pairs = item.NestedDocs
        .Select(d => (d.Year, d.CategoryId))
        .Distinct()
        .ToArray();

    while (sw.Elapsed < timeout)
    {
        var allMatched = true;
        foreach (var (year, categoryId) in pairs)
        {
            var nestedFilter = nestedBuilder.Compound().Must(
                nestedBuilder.Equals(d => d.Year, year),
                nestedBuilder.Equals(d => d.CategoryId, categoryId));

            var searchDef = searchBuilder.Compound().Filter([
                searchBuilder.In(x => x.Id, [item.Id]),
                searchBuilder.EmbeddedDocument(x => x.NestedDocs, nestedFilter)
            ]);

            var count = collection.Aggregate()
                .Search(searchDef, new SearchOptions<TDocument> { IndexName = indexName })
                .Count()
                .FirstOrDefault()?.Count ?? 0;

            if (count == 0) { allMatched = false; break; }
        }

        if (allMatched) return;
        await Task.Delay(500);
    }

    throw new TimeoutException("Nested field indexing not complete");
}
```

### Per-Test Cleanup (Atlas Search)

```csharp
private async Task ClearTestDataAsync()
{
    var collection = dbContext.GetCollection<TDocument>();

    // 1. Record existing IDs for removal tracking
    var existingIds = await collection
        .Find(FilterDefinition<TDocument>.Empty)
        .Project(x => x.Id)
        .ToListAsync();

    // 2. Delete all data
    await collection.DeleteManyAsync(FilterDefinition<TDocument>.Empty);

    // 3. Wait for search index to reflect deletions
    await WaitForSearchIndexRemoval(collection, existingIds.ToArray(), indexName);
}
```

**Critical**: Use `DeleteManyAsync` + wait for removal. **Never** `DropDatabaseAsync` — that destroys the search indexes, requiring expensive re-creation.

## Atlas Search Index Definition

### SearchIndexDefinitionBuilder\<T\>

Strongly-typed fluent builder for Atlas Search index definitions. Resolves field names via `ExpressionFieldDefinition<T>` + `BsonSerializer`, respecting BSON naming conventions.

```csharp
public class SearchIndexDefinitionBuilder<TDocument>
{
    private string _analyzer = "lucene.standard";
    private bool _dynamic = true;
    private readonly List<BsonDocument> _mappings = new();
    private readonly List<BsonDocument> _analyzers = new();

    public SearchIndexDefinitionBuilder<TDocument> WithAnalyzer(string analyzer);
    public SearchIndexDefinitionBuilder<TDocument> WithAnalyzer(AtlasSearchAnalyzer analyzer);
    public SearchIndexDefinitionBuilder<TDocument> Dynamic(bool dynamic = true);

    // Field types
    public SearchIndexDefinitionBuilder<TDocument> TokenField<TField>(
        Expression<Func<TDocument, TField>> field);
    public SearchIndexDefinitionBuilder<TDocument> StringField<TField>(
        Expression<Func<TDocument, TField>> field, AtlasSearchAnalyzer? analyzer = null);
    public SearchIndexDefinitionBuilder<TDocument> NumberField<TField>(
        Expression<Func<TDocument, TField>> field);
    public SearchIndexDefinitionBuilder<TDocument> BooleanField<TField>(
        Expression<Func<TDocument, TField>> field);
    public SearchIndexDefinitionBuilder<TDocument> DateField<TField>(
        Expression<Func<TDocument, TField>> field);

    // Nested structures
    public SearchIndexDefinitionBuilder<TDocument> DocumentField<TField>(
        Expression<Func<TDocument, TField>> field,
        Action<SearchIndexDefinitionBuilder<TField>> configure);
    public SearchIndexDefinitionBuilder<TDocument> EmbeddedDocumentsField<TField>(
        Expression<Func<TDocument, IEnumerable<TField>>> field,
        Action<SearchIndexDefinitionBuilder<TField>> configure);

    // Vector
    public SearchIndexDefinitionBuilder<TDocument> VectorField(
        Expression<Func<TDocument, IReadOnlyList<float>?>> field,
        int numDimensions,
        VectorSimilarity similarity = VectorSimilarity.Cosine);

    // Custom analyzer
    public SearchIndexDefinitionBuilder<TDocument> WithCustomAnalyzer(
        string name, string tokenizerType,
        Dictionary<string, string>? charMappings = null,
        string[]? tokenFilters = null);

    public BsonDocument Build();
}
```

#### Field Name Resolution

```csharp
private static string GetFieldName<TField>(Expression<Func<TDocument, TField>> expression)
{
    var fieldDef = new ExpressionFieldDefinition<TDocument, TField>(expression);
    var serializer = BsonSerializer.LookupSerializer<TDocument>();
    var registry = BsonSerializer.SerializerRegistry;

    return fieldDef.Render(
        new RenderArgs<TDocument>(serializer, registry)
    ).FieldName;
}
```

Uses the same mechanism as `Builders<T>` — respects `[BsonElement]` attributes and registered conventions.

#### Usage Example

```csharp
var definition = new SearchIndexDefinitionBuilder<Product>()
    .WithAnalyzer(AtlasSearchAnalyzer.CaseSensitive)
    .Dynamic(false)
    // Basic fields
    .TokenField(x => x.Id)
    .TokenField(x => x.CategoryIds)
    .BooleanField(x => x.IsActive)
    .StringField(x => x.Description, AtlasSearchAnalyzer.CaseSensitive)
    .DateField(x => x.UpdatedOn)
    // Vector field
    .VectorField(x => x.Embedding, 1024, VectorSimilarity.Cosine)
    // Nested document
    .DocumentField(x => x.Content, content => content
        .StringField(c => c.Title, AtlasSearchAnalyzer.CaseSensitive)
        .StringField(c => c.Body, AtlasSearchAnalyzer.CaseSensitive)
    )
    // Embedded documents array
    .EmbeddedDocumentsField(x => x.Tags, tag => tag
        .TokenField(t => t.Id)
        .TokenField(t => t.Type)
    )
    .Build();
```

### AtlasSearchAnalyzer

Strongly-typed record for analyzer definitions with predefined constants and implicit string conversion:

```csharp
public sealed record AtlasSearchAnalyzer
{
    public required string Name { get; init; }
    public bool IsCustom { get; init; }
    public string? TokenizerType { get; init; }
    public Dictionary<string, string>? CharMappings { get; init; }
    public string[]? TokenFilters { get; init; }

    // Built-in Lucene analyzers
    public static AtlasSearchAnalyzer LuceneStandard { get; } = new() { Name = "lucene.standard" };
    public static AtlasSearchAnalyzer LuceneKeyword { get; } = new() { Name = "lucene.keyword" };

    // Custom analyzer example: case-sensitive with char mappings, no lowercase
    public static AtlasSearchAnalyzer CaseSensitive { get; } = new()
    {
        Name = "case_sensitive_analyzer",
        IsCustom = true,
        TokenizerType = "standard",
        CharMappings = new Dictionary<string, string>
        {
            ["+"] = "_PLUS_",
            ["-"] = "_HYPHEN_",
            ["="] = "_EQUALS_",
            // ... special characters that Lucene would otherwise interpret
        },
        TokenFilters = []   // No "lowercase" → preserves case
    };

    public static implicit operator string(AtlasSearchAnalyzer analyzer) => analyzer.Name;
}
```

When `IsCustom` is true and the analyzer is passed to `WithAnalyzer()`, the builder automatically registers it via `WithCustomAnalyzer()`.

### AtlasSearchIndexManager

Manages the full lifecycle of Atlas Search indexes:

```csharp
public class AtlasSearchIndexManager(
    IMongoCollection<TDocument> collection,
    IOptions<EmbeddingOptions> embeddingOptions,
    ILogger<AtlasSearchIndexManager> logger)
{
    // Ensure collection exists + create index if not present
    public async Task EnsureIndexesAsync(CancellationToken ct = default)
    {
        await EnsureCollectionExistsAsync(ct);
        await CreateSearchIndexIfNotExistsAsync(ct);
    }

    // Create index (idempotent — checks existence first)
    private async Task CreateSearchIndexIfNotExistsAsync(CancellationToken ct)
    {
        var existingIndexes = await collection.SearchIndexes.ListAsync(cancellationToken: ct);
        var indexList = await existingIndexes.ToListAsync(ct);

        if (indexList.Any(idx => idx["name"] == IndexName)) return;

        var definition = BuildSearchIndexDefinition(embeddingOptions.Value);
        await collection.SearchIndexes.CreateOneAsync(definition, IndexName, ct);
    }

    // Update existing index definition (or create if not exists)
    public async Task UpdateSearchIndexAsync(CancellationToken ct = default)
    {
        var existingIndexes = await collection.SearchIndexes.ListAsync(cancellationToken: ct);
        var indexList = await existingIndexes.ToListAsync(ct);

        var definition = BuildSearchIndexDefinition(embeddingOptions.Value);

        if (indexList.Any(idx => idx["name"] == IndexName))
            await collection.SearchIndexes.UpdateAsync(IndexName, definition, cancellationToken: ct);
        else
            await collection.SearchIndexes.CreateOneAsync(definition, IndexName, ct);
    }

    // Force recreate (drop + create — has downtime)
    public async Task RecreateSearchIndexAsync(CancellationToken ct = default)
    {
        var existingIndexes = await collection.SearchIndexes.ListAsync(cancellationToken: ct);
        var indexList = await existingIndexes.ToListAsync(ct);

        if (indexList.Any(idx => idx["name"] == IndexName))
        {
            await collection.SearchIndexes.DropOneAsync(IndexName, cancellationToken: ct);
            await Task.Delay(TimeSpan.FromSeconds(2), ct);
        }

        var definition = BuildSearchIndexDefinition(embeddingOptions.Value);
        await collection.SearchIndexes.CreateOneAsync(definition, IndexName, ct);
    }

    // Query current index status
    public async Task<BsonDocument?> GetIndexStatusAsync(CancellationToken ct = default)
    {
        var existingIndexes = await collection.SearchIndexes.ListAsync(cancellationToken: ct);
        var indexList = await existingIndexes.ToListAsync(ct);
        return indexList.FirstOrDefault(idx => idx["name"] == IndexName);
    }
}
```

Key operations:
- **`EnsureIndexesAsync()`** — call at application startup; idempotent.
- **`UpdateAsync()`** — update definition without drop; Atlas re-indexes in background.
- **`RecreateSearchIndexAsync()`** — drop + create; use when update isn't sufficient.
- **`GetIndexStatusAsync()`** — returns `BsonDocument` with `status`, `queryable` fields.

#### Ensuring Collection Exists

MongoDB requires the collection to exist before creating a Search Index:

```csharp
private async Task EnsureCollectionExistsAsync(CancellationToken ct)
{
    var collectionName = collection.CollectionNamespace.CollectionName;
    var database = collection.Database;

    var filter = new BsonDocument("name", collectionName);
    var collections = await database.ListCollectionNamesAsync(
        new ListCollectionNamesOptions { Filter = filter }, ct);

    if (!await collections.AnyAsync(ct))
        await database.CreateCollectionAsync(collectionName, cancellationToken: ct);
}
```

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
| **Container (standard)** | `new MongoDbBuilder().WithImage("mongo:8.0").WithReplicaSet().Build()` |
| **Container (Atlas Local)** | `new MongoDbBuilder().WithImage("mongodb/mongodb-atlas-local:8.2.4").WithUsername(null).WithPassword(null).WithWaitStrategy(Wait.ForUnixContainer().UntilContainerIsHealthy()).Build()` |
| **Isolation (standard)** | `DropDatabaseAsync()` in `[SetUp]` |
| **Isolation (Atlas Search)** | `DeleteManyAsync(Filter.Empty)` + wait for index removal in `[SetUp]` |
| **Get collection** | `server.GetRequiredService<DbContext>().GetCollection<TDto>()` |
| **Filter** | `Builders<T>.Filter.Eq(x => x.Id, id)` |
| **Update** | `Builders<T>.Update.Set(x => x.Name, name)` |
| **Insert** | `collection.InsertOneAsync(new TDto { ... })` |
| **BSON snapshot** | `obj.ToBsonDocument(typeof(TBase)).ToString()` → `Verifier.Verify()` |
| **Coverage guard** | Reflection to assert all types have samples |
| **Connection override** | `AddInMemoryCollection` with container `GetConnectionString()` |
| **Create search index** | `collection.SearchIndexes.CreateOneAsync(definition, indexName)` |
| **Update search index** | `collection.SearchIndexes.UpdateAsync(indexName, definition)` |
| **Wait for index READY** | Poll `SearchIndexes.ListAsync()` for `status == "READY"` |
| **Wait for indexing** | Poll `Aggregate().Search(Search.In(x => x.Id, ids)).Count()` |
| **Wait for removal** | Same poll, until count == 0 |
| **Index definition** | `new SearchIndexDefinitionBuilder<T>().TokenField(x => x.Id)...Build()` |
| **Vector field** | `.VectorField(x => x.Embedding, dimensions, VectorSimilarity.Cosine)` |
| **Nested field** | `.EmbeddedDocumentsField(x => x.Items, i => i.TokenField(x => x.Id))` |
| **Custom analyzer** | `AtlasSearchAnalyzer.CaseSensitive` or `.WithCustomAnalyzer(...)` |

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
10. **Use Atlas Local for search tests** — `mongodb/mongodb-atlas-local:8.2.4` supports `$search` and `$vectorSearch`.
11. **Never `DropDatabaseAsync` with search indexes** — use `DeleteManyAsync` + wait for index removal instead.
12. **Create search indexes once per test run** — indexes are expensive; share via singleton container.
13. **Always wait for search indexing** — Atlas Search is eventually consistent; poll with `$search` after insert.
14. **Wait for nested fields separately** — `EmbeddedDocument` fields index independently; verify with actual query filters.

## Additional Resources

### Reference Files

- **`references/reviewer-checklist.md`** — Reviewer checklist for verifying MongoDB test compliance
