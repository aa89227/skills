// MongoDB C# Driver v3.7.0 — CRUD Operations
// Demonstrates: InsertOne/Many, Find, FindOneAndUpdate,
//   UpdateOne/Many, ReplaceOne (upsert), DeleteOne/Many, Count

using MongoDB.Driver;

// Assumes: collection is IMongoCollection<Person>

// --- Insert ---

await collection.InsertOneAsync(new Person { Name = "Alice", Age = 30, Email = "a@b.com" });

var people = new List<Person>
{
    new() { Name = "Bob",     Age = 25, Email = "b@b.com" },
    new() { Name = "Charlie", Age = 35, Email = "c@b.com" }
};
await collection.InsertManyAsync(people);

// --- Find ---

// Find all
var all = await collection.Find(_ => true).ToListAsync();

// Find with lambda filter
var adults = await collection.Find(p => p.Age >= 18).ToListAsync();

// Find with Builders — sort + limit
var filter = Builders<Person>.Filter.Gte(p => p.Age, 18);
var result = await collection.Find(filter)
    .Sort(Builders<Person>.Sort.Descending(p => p.Age))
    .Limit(10)
    .ToListAsync();

// Find one
var person = await collection.Find(p => p.Name == "Alice").FirstOrDefaultAsync();

// Count
var count = await collection.CountDocumentsAsync(p => p.Age > 20);

// --- Update ---

var aliceFilter = Builders<Person>.Filter.Eq(p => p.Name, "Alice");

// Update one field
await collection.UpdateOneAsync(aliceFilter, Builders<Person>.Update.Set(p => p.Age, 31));

// Update multiple fields
await collection.UpdateOneAsync(aliceFilter,
    Builders<Person>.Update
        .Set(p => p.Age, 31)
        .Set(p => p.Status, "Active")
        .Inc(p => p.Score, 10));

// Update many
await collection.UpdateManyAsync(
    p => p.Status == "Inactive",
    Builders<Person>.Update.Set(p => p.Status, "Archived"));

// FindOneAndUpdate — returns updated document
var updated = await collection.FindOneAndUpdateAsync(
    aliceFilter,
    Builders<Person>.Update.Set(p => p.Age, 32),
    new FindOneAndUpdateOptions<Person> { ReturnDocument = ReturnDocument.After });

// --- Replace ---

var id = updated!.Id;
var replacement = new Person { Id = id, Name = "Alice Updated", Age = 32, Email = "a@b.com" };
await collection.ReplaceOneAsync(Builders<Person>.Filter.Eq(p => p.Id, id), replacement);

// Upsert
await collection.ReplaceOneAsync(
    Builders<Person>.Filter.Eq(p => p.Name, "NewUser"),
    new Person { Name = "NewUser", Age = 20, Email = "n@b.com" },
    new ReplaceOptions { IsUpsert = true });

// --- Delete ---

await collection.DeleteOneAsync(p => p.Name == "Charlie");

var deleteResult = await collection.DeleteManyAsync(p => p.Age < 18);
Console.WriteLine($"Deleted {deleteResult.DeletedCount} documents");
