// MongoDB C# Driver v3.7.0 — Transactions & DI Registration
// Demonstrates: WithTransactionAsync (recommended), manual transaction,
//   ASP.NET Core DI registration

using MongoDB.Bson;
using MongoDB.Driver;

// --- WithTransactionAsync (recommended: handles retries automatically) ---

using var session = await client.StartSessionAsync();

await session.WithTransactionAsync(async (s, ct) =>
{
    var people = client.GetDatabase("mydb").GetCollection<Person>("people");
    var logs   = client.GetDatabase("mydb").GetCollection<BsonDocument>("logs");

    await people.InsertOneAsync(s, new Person { Name = "Bob", Age = 25, Email = "b@b.com" }, cancellationToken: ct);
    await logs.InsertOneAsync(s, new BsonDocument("action", "user_created"), cancellationToken: ct);

    return "done";
},
cancellationToken: CancellationToken.None);

// IMPORTANT: Always rethrow exceptions inside WithTransactionAsync to avoid infinite retry loops.

// --- Manual transaction (lower-level control) ---

using var session2 = await client.StartSessionAsync();
session2.StartTransaction();
try
{
    var people = client.GetDatabase("mydb").GetCollection<Person>("people");
    var logs   = client.GetDatabase("mydb").GetCollection<BsonDocument>("logs");

    await people.InsertOneAsync(session2, new Person { Name = "Alice", Age = 30, Email = "a@b.com" });
    await logs.InsertOneAsync(session2, new BsonDocument("action", "user_created"));

    await session2.CommitTransactionAsync();
}
catch (Exception)
{
    await session2.AbortTransactionAsync();
    throw;
}

// --- ASP.NET Core DI registration ---
// MongoClient is thread-safe and manages its own connection pool — always register as singleton.

// In Program.cs:
builder.Services.AddSingleton<IMongoClient>(sp =>
    new MongoClient(builder.Configuration.GetConnectionString("MongoDB")));

builder.Services.AddSingleton(sp =>
    sp.GetRequiredService<IMongoClient>().GetDatabase("mydb"));

builder.Services.AddSingleton(sp =>
    sp.GetRequiredService<IMongoDatabase>().GetCollection<Person>("people"));

// Supporting type
public class Person
{
    public string Id    { get; set; }
    public string Name  { get; set; }
    public int    Age   { get; set; }
    public string Email { get; set; }
}
