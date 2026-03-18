---
name: mongodb-driver
description: |
  Use when working with MongoDB in C#/.NET: MongoClient, IMongoCollection CRUD, Builders (Filter/Update/Sort/Projection),
  LINQ queries, aggregation pipelines, Atlas Search ($search, phrase, compound, text, autocomplete),
  Vector Search ($vectorSearch), RankFusion (hybrid search), transactions, change streams, GridFS,
  BSON serialization (attributes, BsonClassMap, conventions), search indexes, DI registration.
  Trigger phrases: "MongoDB", "MongoClient", "IMongoCollection", "Builders.Filter", "Builders.Update",
  "BsonDocument", "BSON attribute", "MongoDB LINQ", "aggregation pipeline", "MongoDB transaction",
  "Atlas Search", "vector search", "rankFusion", "phrase search", "GridFS", "BsonClassMap".
license: MIT
metadata:
  author: aa89227
  version: "2.0"
  driver-version: "3.7.0"
  tags: ["csharp", "dotnet", "mongodb", "nosql", "database", "driver"]
  trigger_keywords: ["MongoDB", "MongoClient", "IMongoCollection", "BsonDocument", "Builders", "MongoDB.Driver", "Atlas Search", "VectorSearch", "RankFusion", "GridFS", "BsonClassMap"]
---

## Auto-Trigger Scenarios

This skill activates when:
- User writes or reviews C# code using `MongoDB.Driver`
- Code references `MongoClient`, `IMongoCollection<T>`, `Builders<T>`
- User asks about MongoDB CRUD, aggregation, transactions, or BSON serialization
- Files contain `using MongoDB.Driver` or `using MongoDB.Bson`

# MongoDB C# Driver — v3.7.0

> **Version:** Verified against `v3.7.0` tag of `mongodb/mongo-csharp-driver`.

## Quick Reference

**NuGet:** `MongoDB.Driver` (includes `MongoDB.Bson`)
**Namespace:** `MongoDB.Driver`, `MongoDB.Bson`
**Requires:** .NET 8+ (recommended), .NET Standard 2.1
**Docs:** `mongodb.com/docs/drivers/csharp/current/`
**API Reference:** `mongodb.github.io/mongo-csharp-driver/3.7.0/api/`

## NuGet Packages

| Package | Purpose |
|---|---|
| `MongoDB.Driver` | Main driver (includes MongoDB.Bson) |
| `MongoDB.Driver.Encryption` | Client-side field level encryption |
| `MongoDB.Driver.Authentication.AWS` | AWS IAM authentication |

## 1. Connection & Setup

```shell
dotnet add package MongoDB.Driver
```

### MongoClient constructors

```csharp
using MongoDB.Driver;

// Connection string
var client = new MongoClient("mongodb://localhost:27017");

// MongoUrl
var url = new MongoUrl("mongodb+srv://user:pass@cluster.mongodb.net/mydb");
var client = new MongoClient(url);

// MongoClientSettings (full control)
var settings = MongoClientSettings.FromConnectionString("mongodb+srv://...");
settings.ServerApi = new ServerApi(ServerApiVersion.V1);
settings.MaxConnectionPoolSize = 100;
var client = new MongoClient(settings);
```

### Get database and collection

```csharp
var database = client.GetDatabase("mydb");
var collection = database.GetCollection<Person>("people");

// Untyped (BsonDocument)
var untypedCollection = database.GetCollection<BsonDocument>("people");
```

## 2. BSON Serialization & Document Model

### Typed document with attributes

```csharp
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

public class Person
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; }

    [BsonElement("name")]
    public string Name { get; set; }

    [BsonElement("age")]
    public int Age { get; set; }

    [BsonIgnore]
    public string CalculatedField { get; set; }

    [BsonIgnoreIfNull]
    public string? NickName { get; set; }

    [BsonIgnoreIfDefault]
    public int Score { get; set; }

    [BsonRequired]
    public string Email { get; set; }

    [BsonExtraElements]
    public BsonDocument ExtraFields { get; set; }

    [BsonDefaultValue("Unknown")]
    public string Status { get; set; }
}
```

### Key BSON attributes

| Attribute | Purpose |
|---|---|
| `[BsonId]` | Marks the `_id` field |
| `[BsonElement("name")]` | Maps property to BSON field name |
| `[BsonRepresentation(BsonType.ObjectId)]` | Stores `string` as `ObjectId` |
| `[BsonIgnore]` | Excludes property from serialization |
| `[BsonIgnoreIfNull]` | Omits field if value is null |
| `[BsonIgnoreIfDefault]` | Omits field if value is default |
| `[BsonRequired]` | Field must be present in BSON |
| `[BsonExtraElements]` | Captures unmapped fields |
| `[BsonDefaultValue(val)]` | Default when field is missing |
| `[BsonDiscriminator]` | Polymorphic type discriminator |
| `[BsonKnownTypes]` | Registers known derived types |
| `[BsonNoId]` | Document has no `_id` field |
| `[BsonConstructor]` | Marks constructor for deserialization |

### BsonDocument (untyped)

```csharp
using MongoDB.Bson;

var doc = new BsonDocument
{
    { "name", "Alice" },
    { "age", 30 },
    { "tags", new BsonArray { "admin", "user" } }
};
```

## 3. CRUD Operations

### Insert

```csharp
// Insert one
await collection.InsertOneAsync(new Person { Name = "Alice", Age = 30, Email = "a@b.com" });

// Insert many
var people = new List<Person>
{
    new() { Name = "Bob", Age = 25, Email = "b@b.com" },
    new() { Name = "Charlie", Age = 35, Email = "c@b.com" }
};
await collection.InsertManyAsync(people);
```

### Find / Read

```csharp
// Find all
var all = await collection.Find(_ => true).ToListAsync();

// Find with filter (lambda)
var adults = await collection.Find(p => p.Age >= 18).ToListAsync();

// Find with Builders
var filter = Builders<Person>.Filter.Gte(p => p.Age, 18);
var result = await collection.Find(filter)
    .Sort(Builders<Person>.Sort.Descending(p => p.Age))
    .Limit(10)
    .ToListAsync();

// Find one
var person = await collection.Find(p => p.Name == "Alice").FirstOrDefaultAsync();

// Count
var count = await collection.CountDocumentsAsync(p => p.Age > 20);
```

### Update

```csharp
var filter = Builders<Person>.Filter.Eq(p => p.Name, "Alice");

// Update one field
var update = Builders<Person>.Update.Set(p => p.Age, 31);
await collection.UpdateOneAsync(filter, update);

// Update multiple fields
var update = Builders<Person>.Update
    .Set(p => p.Age, 31)
    .Set(p => p.Status, "Active")
    .Inc(p => p.Score, 10);
await collection.UpdateOneAsync(filter, update);

// Update many
await collection.UpdateManyAsync(
    p => p.Status == "Inactive",
    Builders<Person>.Update.Set(p => p.Status, "Archived"));

// FindOneAndUpdate (returns the document)
var options = new FindOneAndUpdateOptions<Person>
{
    ReturnDocument = ReturnDocument.After
};
var updated = await collection.FindOneAndUpdateAsync(filter, update, options);
```

### Replace

```csharp
var filter = Builders<Person>.Filter.Eq(p => p.Id, id);
var replacement = new Person { Id = id, Name = "Alice Updated", Age = 32, Email = "a@b.com" };
await collection.ReplaceOneAsync(filter, replacement);

// Upsert
await collection.ReplaceOneAsync(filter, replacement, new ReplaceOptions { IsUpsert = true });
```

### Delete

```csharp
// Delete one
await collection.DeleteOneAsync(p => p.Name == "Alice");

// Delete many
var result = await collection.DeleteManyAsync(p => p.Age < 18);
Console.WriteLine($"Deleted {result.DeletedCount} documents");
```

## 4. Builders — Filter, Update, Sort, Projection

### Filter builders

```csharp
var f = Builders<Person>.Filter;

var filter = f.Eq(p => p.Name, "Alice");           // ==
var filter = f.Ne(p => p.Name, "Alice");           // !=
var filter = f.Gt(p => p.Age, 30);                 // >
var filter = f.Gte(p => p.Age, 30);                // >=
var filter = f.Lt(p => p.Age, 30);                 // <
var filter = f.Lte(p => p.Age, 30);                // <=
var filter = f.In(p => p.Status, new[] { "A", "B" }); // $in
var filter = f.Regex(p => p.Name, new BsonRegularExpression("^A")); // regex

// Combine
var filter = f.And(f.Gte(p => p.Age, 18), f.Lt(p => p.Age, 65));
var filter = f.Or(f.Eq(p => p.Status, "A"), f.Eq(p => p.Status, "B"));
var filter = f.Not(f.Eq(p => p.Status, "Inactive"));

// Array operators
var filter = f.AnyEq(p => p.Tags, "admin");        // array contains
var filter = f.Size(p => p.Tags, 3);               // array size
var filter = f.ElemMatch(p => p.Grades, g => g.Score > 90); // nested match

// Exists / Type
var filter = f.Exists(p => p.NickName);
var filter = f.Type(p => p.Age, BsonType.Int32);

// Empty (matches all)
var filter = f.Empty;
```

### Update builders

```csharp
var u = Builders<Person>.Update;

u.Set(p => p.Name, "New Name")          // set field
u.Unset(p => p.NickName)                // remove field
u.Inc(p => p.Score, 5)                  // increment
u.Mul(p => p.Score, 2)                  // multiply
u.Min(p => p.Score, 0)                  // set to min
u.Max(p => p.Score, 100)                // set to max
u.CurrentDate(p => p.UpdatedAt)         // set to current date
u.Rename("old_field", "new_field")      // rename field
u.SetOnInsert(p => p.CreatedAt, DateTime.UtcNow) // set only on upsert insert

// Array operators
u.Push(p => p.Tags, "newTag")           // append to array
u.PushEach(p => p.Tags, new[] { "a", "b" }) // append multiple
u.AddToSet(p => p.Tags, "unique")       // add if not exists
u.Pull(p => p.Tags, "removeMe")         // remove from array
u.PopFirst(p => p.Tags)                 // remove first element
u.PopLast(p => p.Tags)                  // remove last element

// Combine multiple updates
var update = u.Combine(
    u.Set(p => p.Name, "Updated"),
    u.Inc(p => p.Score, 10));
```

### Sort builders

```csharp
var s = Builders<Person>.Sort;

var sort = s.Ascending(p => p.Name);
var sort = s.Descending(p => p.Age);
var sort = s.Combine(s.Ascending(p => p.Name), s.Descending(p => p.Age));
```

### Projection builders

```csharp
var p = Builders<Person>.Projection;

var projection = p.Include(x => x.Name).Include(x => x.Age);
var projection = p.Exclude(x => x.Email);

var result = await collection.Find(_ => true)
    .Project<BsonDocument>(projection)
    .ToListAsync();

// Expression projection (returns anonymous type)
var result = await collection.Find(_ => true)
    .Project(x => new { x.Name, x.Age })
    .ToListAsync();
```

## 5. LINQ Queries

```csharp
using MongoDB.Driver.Linq;

var queryable = collection.AsQueryable();

// Basic query
var results = await queryable
    .Where(p => p.Age >= 18 && p.Status == "Active")
    .OrderBy(p => p.Name)
    .Select(p => new { p.Name, p.Age })
    .ToListAsync();

// GroupBy
var grouped = await queryable
    .GroupBy(p => p.Status)
    .Select(g => new { Status = g.Key, Count = g.Count() })
    .ToListAsync();

// Any / All
var hasAdmins = await queryable.AnyAsync(p => p.Tags.Contains("admin"));
```

## 6. Aggregation Pipeline

### Fluent API

```csharp
var result = await collection.Aggregate()
    .Match(p => p.Age >= 18)
    .Group(p => p.Status, g => new
    {
        Status = g.Key,
        Count = g.Count(),
        AvgAge = g.Average(p => p.Age)
    })
    .Sort(Builders<BsonDocument>.Sort.Descending("Count"))
    .ToListAsync();
```

### Pipeline definition (BsonDocument stages)

```csharp
var pipeline = new BsonDocument[]
{
    new("$match", new BsonDocument("age", new BsonDocument("$gte", 18))),
    new("$group", new BsonDocument
    {
        { "_id", "$status" },
        { "count", new BsonDocument("$sum", 1) },
        { "avgAge", new BsonDocument("$avg", "$age") }
    }),
    new("$sort", new BsonDocument("count", -1))
};

var result = await collection.Aggregate<BsonDocument>(pipeline).ToListAsync();
```

### Common aggregation stages

| Stage | Fluent Method | Description |
|---|---|---|
| `$match` | `.Match()` | Filter documents |
| `$group` | `.Group()` | Group and aggregate |
| `$sort` | `.Sort()` / `.SortByDescending()` | Sort results |
| `$project` | `.Project()` | Shape output fields |
| `$limit` | `.Limit()` | Limit results |
| `$skip` | `.Skip()` | Skip results |
| `$unwind` | `.Unwind()` | Deconstruct array field |
| `$lookup` | `.Lookup()` | Join with another collection |
| `$count` | `.Count()` | Count documents |
| `$out` | `.Out()` | Write results to collection |
| `$merge` | `.Merge()` | Merge results into collection |

## 7. Transactions

```csharp
using var session = await client.StartSessionAsync();

session.StartTransaction();
try
{
    var people = client.GetDatabase("mydb").GetCollection<Person>("people");
    var logs = client.GetDatabase("mydb").GetCollection<BsonDocument>("logs");

    await people.InsertOneAsync(session, new Person { Name = "Alice", Age = 30, Email = "a@b.com" });
    await logs.InsertOneAsync(session, new BsonDocument("action", "user_created"));

    await session.CommitTransactionAsync();
}
catch (Exception)
{
    await session.AbortTransactionAsync();
    throw;
}
```

### WithTransactionAsync (recommended — auto retry)

```csharp
using var session = await client.StartSessionAsync();

await session.WithTransactionAsync(async (s, ct) =>
{
    var people = client.GetDatabase("mydb").GetCollection<Person>("people");
    var logs = client.GetDatabase("mydb").GetCollection<BsonDocument>("logs");

    await people.InsertOneAsync(s, new Person { Name = "Bob", Age = 25, Email = "b@b.com" }, cancellationToken: ct);
    await logs.InsertOneAsync(s, new BsonDocument("action", "user_created"), cancellationToken: ct);

    return "done";
},
cancellationToken: CancellationToken.None);
```

> **Important:** When using `WithTransactionAsync`, always **rethrow** exceptions in catch blocks to avoid infinite retry loops.

## 8. Change Streams

```csharp
// Watch collection for changes
using var cursor = await collection.WatchAsync();

await foreach (var change in cursor.ToEnumerable())
{
    Console.WriteLine($"Operation: {change.OperationType}");
    Console.WriteLine($"Document: {change.FullDocument}");
}

// Watch with options
var options = new ChangeStreamOptions
{
    FullDocument = ChangeStreamFullDocumentOption.UpdateLookup
};
var pipeline = new EmptyPipelineDefinition<ChangeStreamDocument<Person>>()
    .Match(c => c.OperationType == ChangeStreamOperationType.Insert);

using var cursor = await collection.WatchAsync(pipeline, options);
```

## 9. Indexes

```csharp
var indexKeys = Builders<Person>.IndexKeys;

// Single field
await collection.Indexes.CreateOneAsync(
    new CreateIndexModel<Person>(indexKeys.Ascending(p => p.Email),
    new CreateIndexOptions { Unique = true }));

// Compound index
await collection.Indexes.CreateOneAsync(
    new CreateIndexModel<Person>(
        indexKeys.Ascending(p => p.Name).Descending(p => p.Age)));

// Text index
await collection.Indexes.CreateOneAsync(
    new CreateIndexModel<Person>(indexKeys.Text(p => p.Name)));

// List indexes
using var cursor = await collection.Indexes.ListAsync();
var indexes = await cursor.ToListAsync();
```

## 10. BulkWrite

```csharp
var models = new WriteModel<Person>[]
{
    new InsertOneModel<Person>(new Person { Name = "Dave", Age = 28, Email = "d@b.com" }),
    new UpdateOneModel<Person>(
        Builders<Person>.Filter.Eq(p => p.Name, "Alice"),
        Builders<Person>.Update.Set(p => p.Age, 32)),
    new DeleteOneModel<Person>(
        Builders<Person>.Filter.Eq(p => p.Name, "Bob")),
    new ReplaceOneModel<Person>(
        Builders<Person>.Filter.Eq(p => p.Name, "Charlie"),
        new Person { Name = "Charlie", Age = 36, Email = "c@b.com" })
};

var result = await collection.BulkWriteAsync(models);
Console.WriteLine($"Inserted: {result.InsertedCount}, Modified: {result.ModifiedCount}, Deleted: {result.DeletedCount}");
```

## 11. Atlas Search ($search stage)

### Text search

```csharp
var searchDef = Builders<Product>.Search;

var results = await collection.Aggregate()
    .Search(
        searchDef.Text(p => p.Description, "wireless headphones"),
        new SearchOptions<Product> { IndexName = "my_search_index" })
    .ToListAsync();
```

### Phrase search (exact ordered sequence)

```csharp
var results = await collection.Aggregate()
    .Search(
        searchDef.Phrase(
            p => p.Description,       // path
            "wireless headphones",     // query (ordered phrase)
            slop: 2),                  // allowable distance between words
        new SearchOptions<Product> { IndexName = "my_search_index" })
    .ToListAsync();

// With full options
var results = await collection.Aggregate()
    .Search(
        searchDef.Phrase(
            p => p.Description,
            "wireless headphones",
            new SearchPhraseOptions<Product>
            {
                Slop = 2,
                Synonyms = "my_synonym_mapping"
            }),
        new SearchOptions<Product> { IndexName = "my_search_index" })
    .ToListAsync();
```

### Compound search (boolean logic)

```csharp
var results = await collection.Aggregate()
    .Search(
        searchDef.Compound()
            .Must(searchDef.Text(p => p.Description, "headphones"))
            .MustNot(searchDef.Text(p => p.Description, "wired"))
            .Should(searchDef.Text(p => p.Brand, "Sony"))
            .Filter(searchDef.Equals(p => p.InStock, true))
            .MinimumShouldMatch(1),
        new SearchOptions<Product> { IndexName = "my_search_index" })
    .ToListAsync();
```

### Autocomplete

```csharp
var results = await collection.Aggregate()
    .Search(
        searchDef.Autocomplete(p => p.Name, "wire"),
        new SearchOptions<Product> { IndexName = "my_autocomplete_index" })
    .ToListAsync();
```

### SearchOptions

```csharp
var searchOptions = new SearchOptions<Product>
{
    IndexName = "my_search_index",          // required: Atlas Search index name
    ScoreDetails = true,                     // include score breakdown in $meta
    ReturnStoredSource = true,               // return stored fields directly (skip full doc lookup)
    CountOptions = new SearchCountOptions    // get total count
    {
        Type = SearchCountType.Total
    },
    Highlight = new SearchHighlightOptions<Product>  // highlight matching terms
    {
        Path = p => p.Description
    },
    Sort = Builders<Product>.Sort.Descending("score"),
    SearchAfter = "eyJ...",                  // cursor-based pagination
};
```

### All search operators

| Operator | Builder Method | Description |
|---|---|---|
| `text` | `.Text(path, query)` | Full-text search |
| `phrase` | `.Phrase(path, query, slop?)` | Exact phrase with optional slop |
| `compound` | `.Compound().Must().Should()...` | Boolean combination |
| `autocomplete` | `.Autocomplete(path, query)` | Prefix/edge-ngram completion |
| `equals` | `.Equals(path, value)` | Exact value match |
| `exists` | `.Exists(path)` | Field existence check |
| `range` | `.Range(path, SearchRange)` | Numeric/date range |
| `regex` | `.Regex(path, pattern)` | Regular expression |
| `wildcard` | `.Wildcard(path, pattern)` | Wildcard pattern |
| `near` | `.Near(path, origin, pivot)` | Proximity scoring (numeric/date/geo) |
| `moreLikeThis` | `.MoreLikeThis(docs)` | Similar documents |
| `queryString` | `.QueryString(defaultPath, query)` | Lucene-style query string |
| `span` | `.Span(spanDef)` | Positional term matching |
| `geoShape` | `.GeoShape(path, relation, geometry)` | Geospatial shape queries |
| `geoWithin` | `.GeoWithin(path, area)` | Geospatial containment |
| `embeddedDocument` | `.EmbeddedDocument(path, op)` | Search within nested arrays |
| `facet` | `.Facet(op, facets)` | Faceted search |

## 12. Vector Search ($vectorSearch stage)

```csharp
float[] embedding = await GetEmbeddingAsync("wireless headphones");

var results = await collection.Aggregate()
    .VectorSearch(
        field: "embedding",                          // vector field name
        queryVector: QueryVector.Create(embedding),  // query vector
        limit: 10,                                   // max results
        options: new VectorSearchOptions<Product>
        {
            IndexName = "my_vector_index",           // vector search index
            NumberOfCandidates = 100,                // ANN candidates (higher = more accurate)
            Filter = Builders<Product>.Filter.Eq(p => p.Category, "electronics"), // pre-filter
            // Exact = true,                         // ENN (exact but slower)
            // AutoEmbeddingModelName = "voyage-4",  // auto-embedding (Atlas)
        })
    .Project(Builders<Product>.Projection.Include(p => p.Name).Include(p => p.Description))
    .ToListAsync();
```

### VectorSearchOptions

| Property | Description |
|---|---|
| `IndexName` | Name of the vector search index |
| `NumberOfCandidates` | ANN candidate pool size (higher = more accurate, slower) |
| `Filter` | Pre-filter with `FilterDefinition<T>` |
| `Exact` | `true` for exact NN (ENN), `false` for approximate NN (default) |
| `AutoEmbeddingModelName` | Model name for Atlas auto-embedding |

## 13. RankFusion (hybrid search — $rankFusion stage)

Combines multiple pipeline results using **Reciprocal Rank Fusion** scoring.

### Named pipelines with custom weights

```csharp
// Define sub-pipelines
var textPipeline = PipelineDefinition<Product, Product>.Create(new[]
{
    PipelineStageDefinitionBuilder.Search(
        Builders<Product>.Search.Phrase(p => p.Description, "wireless headphones"),
        new SearchOptions<Product> { IndexName = "my_search_index" })
});

var vectorPipeline = PipelineDefinition<Product, Product>.Create(new[]
{
    PipelineStageDefinitionBuilder.VectorSearch(
        (FieldDefinition<Product>)"embedding",
        QueryVector.Create(embedding),
        limit: 10,
        new VectorSearchOptions<Product> { IndexName = "my_vector_index" })
});

// Combine with named pipelines + weights
var results = await collection.Aggregate()
    .RankFusion<Product, Product>(
        pipelines: new Dictionary<string, PipelineDefinition<Product, Product>>
        {
            ["text_search"] = textPipeline,
            ["vector_search"] = vectorPipeline,
        },
        weights: new Dictionary<string, double>
        {
            ["text_search"] = 0.3,
            ["vector_search"] = 0.7,
        },
        options: new RankFusionOptions<Product> { ScoreDetails = true })
    .ToListAsync();
```

### Tuple array with weights (auto-named pipeline1, pipeline2...)

```csharp
var results = await collection.Aggregate()
    .RankFusion<Product, Product>(
        pipelinesWithWeights: new[]
        {
            (textPipeline, (double?)0.3),
            (vectorPipeline, (double?)0.7),
        })
    .ToListAsync();
```

### Array without weights (equal weighting)

```csharp
var results = await collection.Aggregate()
    .RankFusion<Product, Product>(
        pipelines: new[] { textPipeline, vectorPipeline })
    .ToListAsync();
```

### RankFusion overloads

| Overload | Parameters | Use Case |
|---|---|---|
| `Dictionary` | `pipelines`, `weights?`, `options?` | Named pipelines with custom weights |
| `Array` | `pipelines[]`, `options?` | Auto-named, equal weight |
| `Tuple Array` | `(Pipeline, Weight?)[]`, `options?` | Auto-named with per-pipeline weights |

## 14. Atlas Search Indexes

```csharp
// Create search index
await collection.SearchIndexes.CreateOneAsync(
    new CreateSearchIndexModel(
        "my_search_index",
        new BsonDocument
        {
            { "mappings", new BsonDocument("dynamic", true) }
        }));

// Create vector search index (strongly-typed)
await collection.SearchIndexes.CreateOneAsync(
    new CreateVectorSearchIndexModel<Product>(
        field: p => p.Embedding,
        name: "my_vector_index",
        dimensions: 1536,
        similarity: VectorSimilarity.Cosine,
        filterFields: p => p.Category));

// Create auto-embedding vector index
await collection.SearchIndexes.CreateOneAsync(
    new CreateAutoEmbeddingVectorSearchIndexModel<Product>(
        field: p => p.Description,
        name: "my_auto_embed_index",
        embeddingModelName: "voyage-4"));

// List search indexes
using var cursor = await collection.SearchIndexes.ListAsync();
var indexes = await cursor.ToListAsync();

// Update search index
await collection.SearchIndexes.UpdateAsync(
    "my_search_index",
    new BsonDocument { { "mappings", new BsonDocument("dynamic", true) } });

// Delete search index
await collection.SearchIndexes.DropOneAsync("my_search_index");
```

## 15. BsonClassMap (fluent mapping)

Alternative to attributes — configure mappings in code:

```csharp
using MongoDB.Bson.Serialization;

BsonClassMap.RegisterClassMap<Person>(cm =>
{
    cm.AutoMap();
    cm.SetIdMember(cm.GetMemberMap(c => c.Id));
    cm.MapMember(c => c.Name).SetElementName("name");
    cm.MapMember(c => c.Email).SetElementName("email").SetIsRequired(true);
    cm.MapMember(c => c.Age).SetElementName("age");
    cm.SetIgnoreExtraElements(true);
    cm.UnmapMember(c => c.CalculatedField);
});
```

## 16. Conventions

Apply serialization rules globally instead of per-class:

```csharp
using MongoDB.Bson.Serialization.Conventions;

// Register a convention pack
var conventionPack = new ConventionPack
{
    new CamelCaseElementNameConvention(),     // PascalCase → camelCase
    new IgnoreExtraElementsConvention(true),  // ignore unknown fields
    new IgnoreIfNullConvention(true),         // omit null fields
    new EnumRepresentationConvention(BsonType.String), // enums as strings
};

// Apply to all types in a namespace
ConventionRegistry.Register(
    "MyAppConventions",
    conventionPack,
    t => t.Namespace?.StartsWith("MyApp.Models") == true);
```

### Common conventions

| Convention | Effect |
|---|---|
| `CamelCaseElementNameConvention` | `FirstName` → `firstName` |
| `IgnoreExtraElementsConvention` | Skip unmapped BSON fields |
| `IgnoreIfNullConvention` | Omit null properties |
| `IgnoreIfDefaultConvention` | Omit default-valued properties |
| `EnumRepresentationConvention` | Store enums as string/int |
| `ImmutableTypeClassMapConvention` | Support immutable types (records) |
| `NoIdMemberConvention` | Exclude `_id` from certain types |

## 17. GridFS (large file storage)

```csharp
using MongoDB.Driver.GridFS;

var bucket = new GridFSBucket(database, new GridFSBucketOptions
{
    BucketName = "attachments",
    ChunkSizeBytes = 1048576  // 1 MB chunks
});

// Upload
using var uploadStream = File.OpenRead("report.pdf");
var fileId = await bucket.UploadFromStreamAsync("report.pdf", uploadStream);

// Download
using var downloadStream = File.Create("downloaded.pdf");
await bucket.DownloadToStreamAsync(fileId, downloadStream);

// Find files
using var cursor = await bucket.FindAsync(
    Builders<GridFSFileInfo>.Filter.Eq(f => f.Filename, "report.pdf"));
var files = await cursor.ToListAsync();

// Delete
await bucket.DeleteAsync(fileId);
```

## 18. Dependency Injection (ASP.NET Core)

```csharp
// Program.cs — register MongoClient as singleton
builder.Services.AddSingleton<IMongoClient>(sp =>
    new MongoClient(builder.Configuration.GetConnectionString("MongoDB")));

builder.Services.AddSingleton(sp =>
    sp.GetRequiredService<IMongoClient>().GetDatabase("mydb"));

builder.Services.AddSingleton(sp =>
    sp.GetRequiredService<IMongoDatabase>().GetCollection<Person>("people"));
```

> **Important:** `MongoClient` is thread-safe and manages its own connection pool — always register as **singleton**.

## v3.7.0 Highlights

- OpenTelemetry tracing support
- Snapshot session with specific `SnapshotTime`
- `Enumerable.Reverse()` support for .NET 10
- Nullable numeric/char filter comparisons
- `KnownSerializerFinder` for expression pre-analysis optimization
- `ConnectAsync` in synchronous path to reduce deadlocks
- Memory allocation optimizations (disposer structs, byte array reuse)

## Cheat Sheet

| Task | Pattern |
|---|---|
| Connect | `new MongoClient("connection-string")` |
| Get collection | `db.GetCollection<T>("name")` |
| Insert | `collection.InsertOneAsync(doc)` |
| Find | `collection.Find(filter).ToListAsync()` |
| Find one | `collection.Find(filter).FirstOrDefaultAsync()` |
| Update one | `collection.UpdateOneAsync(filter, update)` |
| Replace one | `collection.ReplaceOneAsync(filter, replacement)` |
| Delete one | `collection.DeleteOneAsync(filter)` |
| Count | `collection.CountDocumentsAsync(filter)` |
| Filter builder | `Builders<T>.Filter.Eq(x => x.Field, value)` |
| Update builder | `Builders<T>.Update.Set(x => x.Field, value)` |
| Sort builder | `Builders<T>.Sort.Ascending(x => x.Field)` |
| Projection | `Builders<T>.Projection.Include(x => x.Field)` |
| LINQ | `collection.AsQueryable().Where(...).ToListAsync()` |
| Aggregation | `collection.Aggregate().Match(...).Group(...).ToListAsync()` |
| Transaction | `session.WithTransactionAsync(async (s, ct) => { ... })` |
| Index | `collection.Indexes.CreateOneAsync(new CreateIndexModel<T>(...))` |
| BulkWrite | `collection.BulkWriteAsync(writeModels)` |
| Change stream | `collection.WatchAsync()` |
| Atlas Search | `collection.Aggregate().Search(searchDef, options)` |
| Phrase search | `Builders<T>.Search.Phrase(path, query, slop?)` |
| Compound search | `Builders<T>.Search.Compound().Must(...).Should(...)` |
| Vector search | `collection.Aggregate().VectorSearch(field, queryVector, limit, options)` |
| RankFusion | `collection.Aggregate().RankFusion<T,T>(pipelines, weights?, options?)` |
| Search index | `collection.SearchIndexes.CreateOneAsync(model)` |
| GridFS upload | `bucket.UploadFromStreamAsync(filename, stream)` |
| GridFS download | `bucket.DownloadToStreamAsync(fileId, stream)` |
| Convention | `ConventionRegistry.Register(name, pack, filter)` |
| ClassMap | `BsonClassMap.RegisterClassMap<T>(cm => { ... })` |
| DI singleton | `services.AddSingleton<IMongoClient>(new MongoClient(...))` |

## Notes

- **Version:** All examples verified against `v3.7.0` of `mongodb/mongo-csharp-driver`.
- v3.x follows semantic versioning; breaking changes only in major versions.
- `MongoClient` is thread-safe — use a single instance per application (register as singleton in DI).
- All CRUD methods have both sync and async variants (e.g., `InsertOne` / `InsertOneAsync`).
- Use `Builders<T>` for type-safe, refactor-friendly filter/update/sort/projection definitions.
- LINQ provider (Linq3) is the default since v3.0; fully translates to MQL.
- `IClientSession` must only be used with the `MongoClient` that created it.
- `WithTransactionAsync` handles retries automatically — preferred over manual `StartTransaction`.
- `$search` and `$vectorSearch` are separate aggregation stages; `$rankFusion` combines them.
- `RankFusion` is an **extension method** on `IAggregateFluent` (not a direct method).
- Atlas Search requires Atlas Search indexes created via `collection.SearchIndexes`.
- Convention registration should happen **once at startup** before any serialization occurs.
- `GridFSBucket` stores files in `fs.files` + `fs.chunks` collections (configurable via `BucketName`).
