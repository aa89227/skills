# MongoDB Testing — Reviewer Checklist

When reviewing MongoDB test code, you **must** use the Todo tool or create a checklist file to track each item and ensure every check is completed.

## Checklist

### Strongly-Typed Operations
- [ ] Filter, Update, Sort, Projection all use lambda expressions
- [ ] No magic strings (e.g., `Filter.Eq("_id", id)` or `Update.Set("Name", name)`)
- [ ] Follows the `mongodb-strongly-typed` skill rules

### Data Access
- [ ] DbContext / Collection obtained via DI container (`server.GetRequiredService<T>()`)
- [ ] No direct `MongoClient` creation or manual connection string retrieval
- [ ] Insert operations use typed DTOs, not raw `BsonDocument`

### BSON Serialization Testing
- [ ] Serialization shape tests use Verify snapshots (`ToBsonDocument().ToString()` → `Verifier.Verify()`)
- [ ] Sample Factory includes samples for all concrete types
- [ ] Reflection-based coverage guard test exists (auto-detects missing types)
- [ ] New serializable types are accompanied by Sample Factory and snapshot updates

### Testcontainers Setup
- [ ] Singleton container (`Lazy<T>`), not per-test creation
- [ ] MongoDB image version is pinned (e.g., `mongo:8.0`)
- [ ] ReplicaSet is enabled (`.WithReplicaSet()`)
- [ ] Test isolation via `DropDatabaseAsync()`, not container restart

### Atlas Local / Atlas Search Tests
- [ ] Uses `mongodb/mongodb-atlas-local:8.2.4` image (not `mongo:8.0`) when testing `$search` or `$vectorSearch`
- [ ] No auth configured (`.WithUsername(null).WithPassword(null)`)
- [ ] Wait strategy uses `.WithWaitStrategy(Wait.ForUnixContainer().UntilContainerIsHealthy())`
- [ ] Search indexes created once per test run (not per test)
- [ ] Per-test cleanup uses `DeleteManyAsync` + wait for index removal (NOT `DropDatabaseAsync`)
- [ ] Waits for search indexing after data insert (polls `$search` until count matches)
- [ ] Waits for nested document field indexing when using `EmbeddedDocument` queries
- [ ] Timeout and polling intervals are reasonable (2 min for index creation, 30s for data indexing)

### Search Index Definition
- [ ] Uses `SearchIndexDefinitionBuilder<T>` with strongly-typed lambda expressions
- [ ] Custom analyzers registered via `WithCustomAnalyzer()` or `AtlasSearchAnalyzer` record
- [ ] `VectorField` dimensions match the embedding model's output dimensionality
- [ ] `Dynamic(false)` set when using explicit field mappings (avoids indexing unexpected fields)
- [ ] Collection existence ensured before `CreateOneAsync` (MongoDB requires collection to exist)
