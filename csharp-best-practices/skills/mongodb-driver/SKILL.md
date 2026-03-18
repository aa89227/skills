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
  version: "3.0"
  driver-version: "3.7.0"
  tags: ["csharp", "dotnet", "mongodb", "nosql", "database", "driver"]
  trigger_keywords: ["MongoDB", "MongoClient", "IMongoCollection", "BsonDocument", "Builders", "MongoDB.Driver", "Atlas Search", "VectorSearch", "RankFusion", "GridFS", "BsonClassMap"]
---

# MongoDB C# Driver — v3.7.0

> Verified against `v3.7.0` tag of `mongodb/mongo-csharp-driver`.

## Quick Reference

| Item | Value |
|---|---|
| NuGet | `MongoDB.Driver` (includes `MongoDB.Bson`) |
| Namespace | `MongoDB.Driver`, `MongoDB.Bson` |
| Requires | .NET 8+ recommended, .NET Standard 2.1 |
| Docs | `mongodb.com/docs/drivers/csharp/current/` |
| API | `mongodb.github.io/mongo-csharp-driver/3.7.0/api/` |

## Core Rules

- `MongoClient` is thread-safe — register as **singleton** in DI; it manages its own connection pool.
- Prefer `WithTransactionAsync` over manual transactions — handles transient error retries automatically.
- Convention registration must run **once at startup**, before any serialization occurs.
- `$search` and `$vectorSearch` are separate pipeline stages — combine via `$rankFusion`.
- `RankFusion` is an **extension method** on `IAggregateFluent` (not a built-in method).
- LINQ provider (Linq3) is default since v3.0 — fully translates to MQL.
- `IClientSession` must only be used with the `MongoClient` that created it.

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
| BulkWrite | `collection.BulkWriteAsync(writeModels)` |
| Change stream | `collection.WatchAsync()` |
| Index | `collection.Indexes.CreateOneAsync(new CreateIndexModel<T>(...))` |
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

## v3.7.0 Highlights

- OpenTelemetry tracing support
- Snapshot session with specific `SnapshotTime`
- `Enumerable.Reverse()` support for .NET 10
- Nullable numeric/char filter comparisons
- `KnownSerializerFinder` expression pre-analysis optimization
- `ConnectAsync` in synchronous path to reduce deadlocks
- Memory allocation optimizations (disposer structs, byte array reuse)

## Additional Resources

### Example Files

Complete, runnable `.cs` examples in `examples/`:
- **`examples/connection-bson.cs`** — MongoClient constructors, typed model with BSON attributes, BsonDocument, BsonClassMap, Conventions
- **`examples/crud.cs`** — InsertOne/Many, Find, FindOneAndUpdate, UpdateOne/Many, ReplaceOne (upsert), Delete, Count
- **`examples/builders.cs`** — All Filter/Update/Sort/Projection builder operators
- **`examples/aggregation-linq.cs`** — LINQ queries, fluent Aggregate, BsonDocument pipeline, BulkWrite, Index creation, Change Streams
- **`examples/atlas-search.cs`** — Text, Phrase, Compound, Autocomplete, SearchMeta/Facets, SearchOptions
- **`examples/vector-rankfusion.cs`** — VectorSearch, RankFusion (3 overloads), Atlas Search Index management, GridFS
- **`examples/transactions-di.cs`** — WithTransactionAsync, manual transaction, ASP.NET Core DI registration

### Reference Files

Detailed tables and rules in `references/`:
- **`references/bson-serialization.md`** — All BSON attributes, Convention table, BsonClassMap API, polymorphism pattern
- **`references/search-reference.md`** — All 17 search operators, SearchOptions, VectorSearchOptions, RankFusion overloads, index model types
