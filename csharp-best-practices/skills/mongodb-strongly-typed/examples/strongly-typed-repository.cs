// MongoDB C# Driver — Strongly-Typed Repository Example
// Demonstrates: Filter, Update, positional operators ($, $[], $[id]),
//   arrayFilters, Aggregation (Match/Project/Facet/Unwind/Lookup),
//   BulkWrite, strongly-typed Index creation

using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using MongoDB.Driver.Linq;

namespace MyCompany.Data;

public sealed class User
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public required string Id { get; init; }

    public required string Email    { get; init; }
    public required string Name     { get; init; }
    public int Age                  { get; init; }
    public required string[] Tags   { get; init; }
    public bool IsActive            { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? LastLoginAt { get; init; }
}

public sealed class Order
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public required string Id       { get; init; }
    public required string UserId   { get; init; }
    public required decimal Amount  { get; init; }
    public required string Status   { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
}

public sealed class UserRepository
{
    private readonly IMongoCollection<User>          _users;
    private readonly IMongoCollection<Order>         _orders;
    private readonly IMongoCollection<UserWithItems> _usersWithItems;

    public UserRepository(IMongoDatabase database)
    {
        _users          = database.GetCollection<User>("users");
        _orders         = database.GetCollection<Order>("orders");
        _usersWithItems = database.GetCollection<UserWithItems>("users");
    }

    // ========================================
    // FILTER
    // ========================================

    public async Task<User?> FindByEmailAsync(string email, CancellationToken ct = default)
    {
        var filter = Builders<User>.Filter.Eq(x => x.Email, email);
        return await _users.Find(filter).FirstOrDefaultAsync(ct);
    }

    // Compound filter: And
    public async Task<List<User>> FindActiveAdultsAsync(CancellationToken ct = default)
    {
        var filter = Builders<User>.Filter.And(
            Builders<User>.Filter.Eq(x => x.IsActive, true),
            Builders<User>.Filter.Gte(x => x.Age, 18));
        return await _users.Find(filter).ToListAsync(ct);
    }

    // AnyEq — array contains element
    public async Task<List<User>> FindByTagAsync(string tag, CancellationToken ct = default)
    {
        var filter = Builders<User>.Filter.AnyEq(x => x.Tags, tag);
        return await _users.Find(filter).ToListAsync(ct);
    }

    // In — any of the values
    // Nin:  Filter.Nin(x => x.Tags, excludeTags)  — none of the values
    // All:  Filter.All(x => x.Tags, requiredTags) — contains all values
    public async Task<List<User>> FindByTagsAsync(string[] tags, CancellationToken ct = default)
    {
        var filter = Builders<User>.Filter.In(x => x.Tags, tags);
        return await _users.Find(filter).ToListAsync(ct);
    }

    // ========================================
    // UPDATE
    // ========================================

    public async Task<bool> UpdateEmailAsync(string userId, string newEmail, CancellationToken ct = default)
    {
        var filter = Builders<User>.Filter.Eq(x => x.Id, userId);
        var update = Builders<User>.Update.Set(x => x.Email, newEmail);
        var result = await _users.UpdateOneAsync(filter, update, cancellationToken: ct);
        return result.ModifiedCount > 0;
    }

    // Chain multiple update operators
    // AddToSet: Update.AddToSet(x => x.Tags, tag) — add unique element to array
    // Inc:      Update.Inc(x => x.Age, 1)         — increment numeric field
    public async Task<bool> UpdateUserProfileAsync(string userId, string name, int age, CancellationToken ct = default)
    {
        var filter = Builders<User>.Filter.Eq(x => x.Id, userId);
        var update = Builders<User>.Update
            .Set(x => x.Name, name)
            .Set(x => x.Age, age)
            .CurrentDate(x => x.LastLoginAt);
        var result = await _users.UpdateOneAsync(filter, update, cancellationToken: ct);
        return result.ModifiedCount > 0;
    }

    // ========================================
    // ARRAY POSITIONAL OPERATORS
    // ========================================

    // $ — FirstMatchingElement() — update first array element matching the filter
    // $[] — AllElements()        — update all array elements
    public async Task<bool> UpdateFirstMatchingItemAsync(
        string userId, string itemName, string newStatus, CancellationToken ct = default)
    {
        var filter = Builders<UserWithItems>.Filter.And(
            Builders<UserWithItems>.Filter.Eq(x => x.Id, userId),
            Builders<UserWithItems>.Filter.ElemMatch(x => x.Items, i => i.Name == itemName));

        // FirstMatchingElement() → "Items.$"
        var update = Builders<UserWithItems>.Update
            .Set(x => x.Items.FirstMatchingElement().Status, newStatus);

        var result = await _usersWithItems.UpdateOneAsync(filter, update, cancellationToken: ct);
        return result.ModifiedCount > 0;
    }

    // $[identifier] — AllMatchingElements("elem") + arrayFilters
    public async Task<bool> UpdateMatchingItemsAsync(
        string userId, int minQuantity, string newStatus, CancellationToken ct = default)
    {
        var filter = Builders<UserWithItems>.Filter.Eq(x => x.Id, userId);

        // AllMatchingElements("elem") → "Items.$[elem]"
        var update = Builders<UserWithItems>.Update
            .Set(x => x.Items.AllMatchingElements("elem").Status, newStatus);

        var options = new UpdateOptions
        {
            ArrayFilters = new[]
            {
                new BsonDocumentArrayFilterDefinition<Item>(
                    new BsonDocument("elem.Quantity", new BsonDocument("$gte", minQuantity)))
            }
        };

        var result = await _usersWithItems.UpdateOneAsync(filter, update, options, ct);
        return result.ModifiedCount > 0;
    }

    // arrayFilter In/Nin — use custom extension (MongoDB.Driver has no native strongly-typed support)
    // ArrayFilterNin: same structure with Nin operator
    public async Task<bool> UpdateItemsByStatusAsync(
        string userId, string[] statuses, string newStatus, CancellationToken ct = default)
    {
        var filter = Builders<UserWithItems>.Filter.Eq(x => x.Id, userId);
        var update = Builders<UserWithItems>.Update
            .Set(x => x.Items.AllMatchingElements("elem").Status, newStatus);

        var arrayFilter = Builders<UserWithItems>.Filter.ArrayFilterIn(
            x => x.Items,
            item => item.Status,
            statuses,
            "elem");

        var result = await _usersWithItems.UpdateOneAsync(
            filter, update, new UpdateOptions { ArrayFilters = new[] { arrayFilter } }, ct);
        return result.ModifiedCount > 0;
    }

    // ========================================
    // AGGREGATION PIPELINE
    // ========================================

    // Match + SortByDescending + Project
    public async Task<List<UserSummaryDto>> AggregateActiveUsersAsync(CancellationToken ct = default)
    {
        return await _users.Aggregate()
            .Match(x => x.IsActive)
            .SortByDescending(x => x.CreatedAt)
            .Project(x => new UserSummaryDto { Id = x.Id, Email = x.Email, Name = x.Name })
            .ToListAsync(ct);
    }

    // Facet — multiple independent aggregations in a single round-trip
    public async Task<AggregateFacetResults<UserSummaryDto, AgeStats>> GetUserFacetsAsync(CancellationToken ct = default)
    {
        var recentUsersFacet = AggregateFacet.Create("recentUsers",
            PipelineDefinition<User, UserSummaryDto>.Create(new[]
            {
                PipelineStageDefinitionBuilder.Sort(Builders<User>.Sort.Descending(x => x.CreatedAt)),
                PipelineStageDefinitionBuilder.Limit<User>(5),
                PipelineStageDefinitionBuilder.Project(
                    Builders<User>.Projection.Expression(x => new UserSummaryDto
                    {
                        Id = x.Id, Email = x.Email, Name = x.Name
                    }))
            }));

        var ageStatsFacet = AggregateFacet.Create("ageStats",
            PipelineDefinition<User, AgeStats>.Create(new[]
            {
                PipelineStageDefinitionBuilder.Group(
                    (User x) => 1,
                    g => new AgeStats
                    {
                        AverageAge = g.Average(x => x.Age),
                        MinAge     = g.Min(x => x.Age),
                        MaxAge     = g.Max(x => x.Age)
                    })
            }));

        var result = await _users.Aggregate()
            .Facet(recentUsersFacet, ageStatsFacet)
            .FirstOrDefaultAsync(ct);

        return new AggregateFacetResults<UserSummaryDto, AgeStats>(
            result.Facets[0].Output<UserSummaryDto>(),
            result.Facets[1].Output<AgeStats>());
    }

    // Unwind — deconstruct array to separate documents, then group + count
    public async Task<List<TagCount>> GetTagCountsWithUnwindAsync(CancellationToken ct = default)
    {
        return await _users.Aggregate()
            .Unwind<User, TagUnwind>(x => x.Tags)
            .Group(x => x.Tag, g => new TagCount { Tag = g.Key, Count = g.Count() })
            .SortByDescending(x => x.Count)
            .ToListAsync(ct);
    }

    // Lookup + Unwind — join collection then filter on joined field
    public async Task<List<UserOrderDto>> GetUsersWithOrderDetailsAsync(CancellationToken ct = default)
    {
        return await _users.Aggregate()
            .Match(x => x.IsActive)
            .Lookup<User, Order, UserOrderLookup>(
                _orders,
                u => u.Id,
                o => o.UserId,
                result => result.Orders)
            .Unwind<UserOrderLookup, UserOrderUnwind>(x => x.Orders)
            .Match(x => x.Orders.Amount > 100)
            .Project(x => new UserOrderDto
            {
                UserName = x.Name,
                OrderId  = x.Orders.Id,
                Amount   = x.Orders.Amount
            })
            .ToListAsync(ct);
    }

    // ========================================
    // BULK WRITE
    // ========================================

    public async Task<BulkWriteResult<User>> BulkOperationsAsync(CancellationToken ct = default)
    {
        var ops = new List<WriteModel<User>>
        {
            new InsertOneModel<User>(new User
            {
                Id        = ObjectId.GenerateNewId().ToString(),
                Email     = "newuser@example.com",
                Name      = "New User",
                Age       = 25,
                Tags      = ["new"],
                IsActive  = true,
                CreatedAt = DateTimeOffset.UtcNow,
            }),
            new UpdateOneModel<User>(
                Builders<User>.Filter.Eq(x => x.Email, "existing@example.com"),
                Builders<User>.Update.Set(x => x.IsActive, false)),
            new DeleteOneModel<User>(
                Builders<User>.Filter.Eq(x => x.Email, "todelete@example.com"))
        };
        return await _users.BulkWriteAsync(ops, cancellationToken: ct);
    }

    // ========================================
    // INDEXES
    // ========================================

    public async Task CreateIndexesAsync(CancellationToken ct = default)
    {
        await _users.Indexes.CreateManyAsync(new[]
        {
            new CreateIndexModel<User>(
                Builders<User>.IndexKeys.Ascending(x => x.Email),
                new CreateIndexOptions { Unique = true }),
            new CreateIndexModel<User>(
                Builders<User>.IndexKeys.Ascending(x => x.IsActive).Descending(x => x.CreatedAt)),
            new CreateIndexModel<User>(
                Builders<User>.IndexKeys.Text(x => x.Name))
        }, ct);
    }
}

// ---- DTOs & intermediate types ----

public sealed record UserSummaryDto
{
    public required string Id    { get; init; }
    public required string Email { get; init; }
    public required string Name  { get; init; }
}

public sealed record TagCount  { public required string Tag { get; init; } public required int Count { get; init; } }
public sealed record AgeStats  { public required double AverageAge { get; init; } public required int MinAge { get; init; } public required int MaxAge { get; init; } }
public sealed record UserOrderDto { public required string UserName { get; init; } public required string OrderId { get; init; } public required decimal Amount { get; init; } }
public sealed record AggregateFacetResults<T1, T2>(IReadOnlyList<T1> First, IReadOnlyList<T2> Second);

file sealed class Item          { public required string Name { get; init; } public required string Status { get; init; } public int Quantity { get; init; } }
file sealed class UserWithItems : User { public required List<Item> Items { get; init; } }
file sealed class TagUnwind     { public required string Id { get; init; } public required string Tag { get; init; } }
file sealed class UserOrderLookup : User { public required List<Order> Orders { get; init; } }
file sealed class UserOrderUnwind : User { public required Order Orders { get; init; } }
