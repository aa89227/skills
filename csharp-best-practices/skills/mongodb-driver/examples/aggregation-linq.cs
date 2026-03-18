// MongoDB C# Driver v3.7.0 — LINQ & Aggregation Pipeline
// Demonstrates: AsQueryable LINQ, fluent Aggregate(), BsonDocument pipeline,
//   BulkWrite, Index creation, Change Streams

using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Linq;

// Assumes: collection is IMongoCollection<Person>

// --- LINQ (Linq3 — default since v3.0) ---

var queryable = collection.AsQueryable();

// Basic where + order + select
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

// Any
var hasAdmins = await queryable.AnyAsync(p => p.Tags.Contains("admin"));

// --- Aggregation (fluent API) ---

var aggResult = await collection.Aggregate()
    .Match(p => p.Age >= 18)
    .Group(p => p.Status, g => new
    {
        Status  = g.Key,
        Count   = g.Count(),
        AvgAge  = g.Average(p => p.Age)
    })
    .Sort(Builders<BsonDocument>.Sort.Descending("Count"))
    .ToListAsync();

// --- Aggregation (BsonDocument pipeline) ---

var pipeline = new BsonDocument[]
{
    new("$match",  new BsonDocument("age", new BsonDocument("$gte", 18))),
    new("$group",  new BsonDocument
    {
        { "_id",    "$status" },
        { "count",  new BsonDocument("$sum", 1) },
        { "avgAge", new BsonDocument("$avg", "$age") }
    }),
    new("$sort",   new BsonDocument("count", -1))
};

var rawResult = await collection.Aggregate<BsonDocument>(pipeline).ToListAsync();

// --- BulkWrite ---

var writeModels = new WriteModel<Person>[]
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

var bulkResult = await collection.BulkWriteAsync(writeModels);
Console.WriteLine($"Inserted: {bulkResult.InsertedCount}, Modified: {bulkResult.ModifiedCount}, Deleted: {bulkResult.DeletedCount}");

// --- Indexes ---

var indexKeys = Builders<Person>.IndexKeys;

// Single field, unique
await collection.Indexes.CreateOneAsync(
    new CreateIndexModel<Person>(
        indexKeys.Ascending(p => p.Email),
        new CreateIndexOptions { Unique = true }));

// Compound index
await collection.Indexes.CreateOneAsync(
    new CreateIndexModel<Person>(
        indexKeys.Ascending(p => p.Name).Descending(p => p.Age)));

// Text index
await collection.Indexes.CreateOneAsync(
    new CreateIndexModel<Person>(indexKeys.Text(p => p.Name)));

// List indexes
using var indexCursor = await collection.Indexes.ListAsync();
var indexes = await indexCursor.ToListAsync();

// --- Change Streams ---

// Basic watch
using var cursor = await collection.WatchAsync();
await foreach (var change in cursor.ToEnumerable())
{
    Console.WriteLine($"Operation: {change.OperationType}");
    Console.WriteLine($"Document: {change.FullDocument}");
}

// Watch insert operations only, with full document on updates
var changeOptions = new ChangeStreamOptions
{
    FullDocument = ChangeStreamFullDocumentOption.UpdateLookup
};
var changePipeline = new EmptyPipelineDefinition<ChangeStreamDocument<Person>>()
    .Match(c => c.OperationType == ChangeStreamOperationType.Insert);

using var filteredCursor = await collection.WatchAsync(changePipeline, changeOptions);

// Supporting types
public class Person
{
    public string Id { get; set; }
    public string Name { get; set; }
    public int Age { get; set; }
    public string Status { get; set; }
    public string Email { get; set; }
    public List<string> Tags { get; set; } = [];
}
