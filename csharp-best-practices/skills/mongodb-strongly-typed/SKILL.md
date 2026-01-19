---
name: mongodb-strongly-typed
description: MongoDB C# Driver strongly-typed patterns. Use when working with MongoDB collections, filters, updates, projections, aggregations, and indexes using strongly-typed expressions.
license: MIT
metadata:
  author: aa89227
  version: "1.0"
  tags: ["mongodb", "csharp", "database", "strongly-typed"]
---

# MongoDB Strongly-Typed Patterns

## Quick Reference

**Driver version:** MongoDB.Driver 3.x (.NET 8+)

## General Rules

- **Always prefer lambda expressions** over magic strings for type safety and refactoring support.
- Use `Builders<T>.Filter`, `Builders<T>.Update`, `Builders<T>.Sort`, `Builders<T>.Projection` for strongly-typed operations.
- Leverage `IMongoCollection<T>.AsQueryable()` for LINQ support (requires MongoDB.Driver.Linq).
- Use `Builders<T>.IndexKeys` for strongly-typed index definitions.
- Avoid string-based field names (`"FieldName"`) - use lambda expressions (`x => x.FieldName`).
- For complex queries, prefer aggregation pipeline with typed stages.
- For arrayFilters with In/Nin operators, use custom extension methods (see "Advanced: ArrayFilter Extension").

```csharp
// ✅ Strongly-typed (refactor-safe, compile-time checked)
var filter = Builders<User>.Filter.Eq(x => x.Email, "test@example.com");

// ❌ Magic string (runtime errors, no refactoring support)
var filter = Builders<User>.Filter.Eq("Email", "test@example.com");
```

## Core Example (High-density)

```csharp
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MyCompany.Data;

public sealed class User
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public required string Id { get; init; }

    public required string Email { get; init; }
    public required string Name { get; init; }
    public int Age { get; init; }
    public required string[] Tags { get; init; }
    public bool IsActive { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? LastLoginAt { get; init; }
}

public sealed class Order
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public required string Id { get; init; }

    public required string UserId { get; init; }
    public required decimal Amount { get; init; }
    public required string Status { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
}

public sealed class UserRepository
{
    private readonly IMongoCollection<User> _users;
    private readonly IMongoCollection<Order> _orders;
    private readonly IMongoCollection<UserWithItems> _usersWithItems;

    public UserRepository(IMongoDatabase database)
    {
        _users = database.GetCollection<User>("users");
        _orders = database.GetCollection<Order>("orders");
        _usersWithItems = database.GetCollection<UserWithItems>("users");
    }

    // ========================================
    // FILTER: Strongly-typed queries
    // ========================================

    // Find user by email
    public async Task<User?> FindByEmailAsync(string email, CancellationToken ct = default)
    {
        var filter = Builders<User>.Filter.Eq(x => x.Email, email);
        return await _users.Find(filter).FirstOrDefaultAsync(ct);
    }

    // Complex filter: combining conditions
    public async Task<List<User>> FindActiveAdultsAsync(CancellationToken ct = default)
    {
        var filter = Builders<User>.Filter.And(
            Builders<User>.Filter.Eq(x => x.IsActive, true),
            Builders<User>.Filter.Gte(x => x.Age, 18)
        );

        return await _users.Find(filter).ToListAsync(ct);
    }

    //Array filter: contains element.
    public async Task<List<User>> FindByTagAsync(string tag, CancellationToken ct = default)
    {
        var filter = Builders<User>.Filter.AnyEq(x => x.Tags, tag);
        return await _users.Find(filter).ToListAsync(ct);
    }

    //Array filter: In (any of the values).
    /// Nin: Filter.Nin(x => x.Tags, excludeTags) - none of the values
    /// All: Filter.All(x => x.Tags, requiredTags) - contains all values
    public async Task<List<User>> FindByTagsAsync(string[] tags, CancellationToken ct = default)
    {
        var filter = Builders<User>.Filter.In(x => x.Tags, tags);
        return await _users.Find(filter).ToListAsync(ct);
    }

    // ========================================
    // UPDATE: Strongly-typed updates
    // ========================================

    //Update single field.
    public async Task<bool> UpdateEmailAsync(string userId, string newEmail, CancellationToken ct = default)
    {
        var filter = Builders<User>.Filter.Eq(x => x.Id, userId);
        var update = Builders<User>.Update.Set(x => x.Email, newEmail);

        var result = await _users.UpdateOneAsync(filter, update, cancellationToken: ct);
        return result.ModifiedCount > 0;
    }

    //Chain multiple update operations.
    /// AddToSet: Update.AddToSet(x => x.Tags, tag) - add unique element to array
    /// Inc: Update.Inc(x => x.Age, 1) - increment numeric field
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
    // Array Positional Operators ($, $[], $[identifier])
    // ========================================

    //Update first matching array element using $ positional operator.
    /// AllElements: Items.AllElements() => "Items.$[]" - update all array elements
    public async Task<bool> UpdateFirstMatchingItemAsync(string userId, string itemName, string newStatus, CancellationToken ct = default)
    {
        var filter = Builders<UserWithItems>.Filter.And(
            Builders<UserWithItems>.Filter.Eq(x => x.Id, userId),
            Builders<UserWithItems>.Filter.ElemMatch(x => x.Items, i => i.Name == itemName)
        );
        // FirstMatchingElement() => "Items.$"
        var update = Builders<UserWithItems>.Update.Set(x => x.Items.FirstMatchingElement().Status, newStatus);

        var result = await _usersWithItems.UpdateOneAsync(filter, update, cancellationToken: ct);
        return result.ModifiedCount > 0;
    }

    //Update matching array elements using $[identifier] with arrayFilters.
    public async Task<bool> UpdateMatchingItemsAsync(string userId, int minQuantity, string newStatus, CancellationToken ct = default)
    {
        var filter = Builders<UserWithItems>.Filter.Eq(x => x.Id, userId);
        // AllMatchingElements("elem") => "Items.$[elem]"
        var update = Builders<UserWithItems>.Update.Set(x => x.Items.AllMatchingElements("elem").Status, newStatus);

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

    // ========================================
    // Advanced: ArrayFilter with In/Nin (Extension Method)
    // ========================================

    //Update array elements where Status is in specific values (using custom extension).
    /// MongoDB.Driver doesn't natively support strongly-typed In/Nin in arrayFilters.
    /// ArrayFilterNin: same structure with Nin operator - matches NOT in values
    public async Task<bool> UpdateItemsByStatusAsync(string userId, string[] statuses, string newStatus, CancellationToken ct = default)
    {
        var filter = Builders<UserWithItems>.Filter.Eq(x => x.Id, userId);
        var update = Builders<UserWithItems>.Update.Set(x => x.Items.AllMatchingElements("elem").Status, newStatus);

        var arrayFilter = Builders<UserWithItems>.Filter.ArrayFilterIn(
            x => x.Items,
            item => item.Status,
            statuses,
            "elem"
        );

        var options = new UpdateOptions
        {
            ArrayFilters = new[] { arrayFilter }
        };

        var result = await _usersWithItems.UpdateOneAsync(filter, update, options, ct);
        return result.ModifiedCount > 0;
    }

    // ========================================
    // AGGREGATION PIPELINE: Complex queries
    // ========================================

    //Aggregation: Match + Project.
    public async Task<List<UserSummaryDto>> AggregateActiveUsersAsync(CancellationToken ct = default)
    {
        var pipeline = _users.Aggregate()
            .Match(x => x.IsActive)
            .SortByDescending(x => x.CreatedAt)
            .Project(x => new UserSummaryDto
            {
                Id = x.Id,
                Email = x.Email,
                Name = x.Name
            });

        return await pipeline.ToListAsync(ct);
    }

    //Aggregation: Facet (multiple aggregations).
    public async Task<AggregateFacetResults<UserSummaryDto, AgeStats>> GetUserFacetsAsync(CancellationToken ct = default)
    {
        var recentUsersFacet = AggregateFacet.Create("recentUsers",
            PipelineDefinition<User, UserSummaryDto>.Create(
                new[]
                {
                    PipelineStageDefinitionBuilder.Sort(Builders<User>.Sort.Descending(x => x.CreatedAt)),
                    PipelineStageDefinitionBuilder.Limit<User>(5),
                    PipelineStageDefinitionBuilder.Project(
                        Builders<User>.Projection.Expression(x => new UserSummaryDto
                        {
                            Id = x.Id,
                            Email = x.Email,
                            Name = x.Name
                        }))
                }
            )
        );

        var ageStatsFacet = AggregateFacet.Create("ageStats",
            PipelineDefinition<User, AgeStats>.Create(
                new[]
                {
                    PipelineStageDefinitionBuilder.Group(
                        (User x) => 1,
                        g => new AgeStats
                        {
                            AverageAge = g.Average(x => x.Age),
                            MinAge = g.Min(x => x.Age),
                            MaxAge = g.Max(x => x.Age)
                        })
                }
            )
        );

        var result = await _users.Aggregate()
            .Facet(recentUsersFacet, ageStatsFacet)
            .FirstOrDefaultAsync(ct);

        return new AggregateFacetResults<UserSummaryDto, AgeStats>(
            result.Facets[0].Output<UserSummaryDto>(),
            result.Facets[1].Output<AgeStats>()
        );
    }

    //Aggregation: Unwind array to separate documents.
    public async Task<List<TagCount>> GetTagCountsWithUnwindAsync(CancellationToken ct = default)
    {
        return await _users.Aggregate()
            .Unwind<User, TagUnwind>(x => x.Tags)
            .Group(x => x.Tag, g => new TagCount
            {
                Tag = g.Key,
                Count = g.Count()
            })
            .SortByDescending(x => x.Count)
            .ToListAsync(ct);
    }

    //Aggregation: Lookup + Unwind pattern (common for joining).
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
                OrderId = x.Orders.Id,
                Amount = x.Orders.Amount
            })
            .ToListAsync(ct);
    }

    // ========================================
    // BULK WRITE: Batch operations
    // ========================================

    //Bulk write: insert, update, delete.
    public async Task<BulkWriteResult<User>> BulkOperationsAsync(CancellationToken ct = default)
    {
        var bulkOps = new List<WriteModel<User>>
        {
            // Insert
            new InsertOneModel<User>(new User
            {
                Id = ObjectId.GenerateNewId().ToString(),
                Email = "newuser@example.com",
                Name = "New User",
                Age = 25,
                Tags = new[] { "new" },
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow,
                LastLoginAt = null
            }),

            // Update
            new UpdateOneModel<User>(
                Builders<User>.Filter.Eq(x => x.Email, "existing@example.com"),
                Builders<User>.Update.Set(x => x.IsActive, false)
            ),

            // Delete
            new DeleteOneModel<User>(
                Builders<User>.Filter.Eq(x => x.Email, "todelete@example.com")
            )
        };

        return await _users.BulkWriteAsync(bulkOps, cancellationToken: ct);
    }

    // ========================================
    // INDEX DEFINITION: Create indexes
    // ========================================

    //Create indexes using strongly-typed definitions.
    public async Task CreateIndexesAsync(CancellationToken ct = default)
    {
        // Single field index
        var emailIndexModel = new CreateIndexModel<User>(
            Builders<User>.IndexKeys.Ascending(x => x.Email),
            new CreateIndexOptions { Unique = true }
        );

        // Compound index
        var activeCreatedIndexModel = new CreateIndexModel<User>(
            Builders<User>.IndexKeys
                .Ascending(x => x.IsActive)
                .Descending(x => x.CreatedAt)
        );

        // Text index
        var nameTextIndexModel = new CreateIndexModel<User>(
            Builders<User>.IndexKeys.Text(x => x.Name)
        );

        await _users.Indexes.CreateManyAsync(
            new[] { emailIndexModel, activeCreatedIndexModel, nameTextIndexModel },
            ct
        );
    }
}

//User summary DTO.
public sealed record UserSummaryDto
{
    public required string Id { get; init; }
    public required string Email { get; init; }
    public required string Name { get; init; }
}

//Tag count result.
public sealed record TagCount
{
    public required string Tag { get; init; }
    public required int Count { get; init; }
}

//Age statistics result.
public sealed record AgeStats
{
    public required double AverageAge { get; init; }
    public required int MinAge { get; init; }
    public required int MaxAge { get; init; }
}

//Item document for nested array operations.
file sealed class Item
{
    public required string Name { get; init; }
    public required string Status { get; init; }
    public int Quantity { get; init; }
}

//User with items for array positional operators.
file sealed class UserWithItems : User
{
    public required List<Item> Items { get; init; }
}

//Intermediate DTO for Unwind result.
file sealed class TagUnwind
{
    public required string Id { get; init; }
    public required string Tag { get; init; }
}

//Intermediate DTO for Lookup result.
file sealed class UserOrderLookup : User
{
    public required List<Order> Orders { get; init; }
}

//Intermediate DTO for Unwind after Lookup.
file sealed class UserOrderUnwind : User
{
    public required Order Orders { get; init; }
}

//Final DTO for user order details.
public sealed record UserOrderDto
{
    public required string UserName { get; init; }
    public required string OrderId { get; init; }
    public required decimal Amount { get; init; }
}
```

## Advanced: ArrayFilter Extension (In/Nin)

MongoDB.Driver doesn't natively support strongly-typed In/Nin operators in arrayFilters. See `UpdateItemsByStatusAsync` method for ArrayFilterIn usage example.

**Extension implementation:** [ArrayFilterExtensions.md](./ArrayFilterExtensions.md)

## Convention Setup

MongoDB Conventions customize serialization behavior and should be registered once at application startup.

```csharp
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;

public static class MongoConventionSetup
{
    public static void RegisterConventions()
    {
        // CamelCase element names (property Name -> "name")
        var camelCasePack = new ConventionPack
        {
            new CamelCaseElementNameConvention()
        };
        ConventionRegistry.Register("CamelCase", camelCasePack, _ => true);

        // Enum as string (avoids magic numbers)
        BsonSerializer.RegisterSerializer(new CamelCaseEnumStringSerializer<UserTier>());
    }
}

//CamelCase enum serializer (enum value -> "enumValue").
public class CamelCaseEnumStringSerializer<TEnum> : SerializerBase<TEnum>
    where TEnum : struct, Enum
{
    public override TEnum Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
    {
        var value = context.Reader.ReadString();
        return Enum.Parse<TEnum>(value, ignoreCase: true);
    }

    public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, TEnum value)
    {
        var name = value.ToString();
        var camelCase = char.ToLowerInvariant(name[0]) + name[1..];
        context.Writer.WriteString(camelCase);
    }
}
```

## Strongly-Typed ID Conversion

Use record to encapsulate IDs with custom BsonSerializer for type safety.

### Define Strongly-Typed ID

```csharp
//Marker interface for value objects.
public interface ValueObject;

//Strongly-typed User ID.
public sealed record UserId(string Value) : ValueObject
{
    public static UserId New() => new(ObjectId.GenerateNewId().ToString());
    public static UserId Parse(string value) => new(value);
    public override string ToString() => Value;
}

//Strongly-typed Order ID.
public sealed record OrderId(string Value) : ValueObject;
```

### Custom BsonSerializer

```csharp
//UserId serializer - stores as string in MongoDB.
public sealed class UserIdBsonSerializer : SerializerBase<UserId?>
{
    public override UserId? Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
    {
        var type = context.Reader.GetCurrentBsonType();
        if (type == BsonType.Null)
        {
            context.Reader.ReadNull();
            return null;
        }

        return type switch
        {
            BsonType.String => new UserId(context.Reader.ReadString()),
            BsonType.ObjectId => new UserId(context.Reader.ReadObjectId().ToString()),
            _ => throw new FormatException($"Cannot deserialize UserId from BsonType {type}")
        };
    }

    public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, UserId? value)
    {
        if (value == null)
            context.Writer.WriteNull();
        else
            context.Writer.WriteString(value.Value);
    }
}
```

### Register Serializer (at application startup)

```csharp
public static class BsonSerializerRegistration
{
    public static void Register()
    {
        BsonSerializer.RegisterSerializer(new UserIdBsonSerializer());
        BsonSerializer.RegisterSerializer(new OrderIdBsonSerializer());
        // ... other ID serializers
    }
}
```

### Using Strongly-Typed ID in Document

```csharp
public sealed class UserDocument
{
    [BsonId]
    public required UserId Id { get; init; }

    public required string Name { get; init; }
    public required List<OrderId> OrderIds { get; init; }
}

// Use strongly-typed ID in queries
var filter = Builders<UserDocument>.Filter.Eq(x => x.Id, userId);
```

## Cheat Sheet

| Topic | Strongly-Typed | Magic String (avoid) |
|-------|----------------|----------------------|
| **Filter (Equality)** | `Filter.Eq(x => x.Id, id)` | `Filter.Eq("_id", id)` |
| **Filter (Comparison)** | `Filter.Gte(x => x.Age, 18)` | `Filter.Gte("Age", 18)` |
| **Filter (Array)** | `Filter.AnyEq(x => x.Tags, tag)` | `Filter.AnyEq("Tags", tag)` |
| **Filter (In)** | `Filter.In(x => x.Tags, arr)` | `Filter.In("Tags", arr)` |
| **Filter (Nin)** | `Filter.Nin(x => x.Tags, arr)` | `Filter.Nin("Tags", arr)` |
| **Filter (All)** | `Filter.All(x => x.Tags, arr)` | `Filter.All("Tags", arr)` |
| **Update (Set)** | `Update.Set(x => x.Name, name)` | `Update.Set("Name", name)` |
| **Update (Inc)** | `Update.Inc(x => x.Age, 1)` | `Update.Inc("Age", 1)` |
| **Update (Array)** | `Update.AddToSet(x => x.Tags, tag)` | `Update.AddToSet("Tags", tag)` |
| **Update ($)** | `x.Items.FirstMatchingElement()` | `"Items.$"` |
| **Update ($[])** | `x.Items.AllElements()` | `"Items.$[]"` |
| **Update ($[id])** | `x.Items.AllMatchingElements("id")` | `"Items.$[id]"` |
| **ArrayFilter (In)** | `ArrayFilterIn(x => x.Items, i => i.Status, arr, "id")` | `new BsonDocument("id.Status", new BsonDocument("$in", arr))` |
| **ArrayFilter (Nin)** | `ArrayFilterNin(x => x.Items, i => i.Status, arr, "id")` | `new BsonDocument("id.Status", new BsonDocument("$nin", arr))` |
| **Sort (Ascending)** | `Sort.Ascending(x => x.CreatedAt)` | `Sort.Ascending("CreatedAt")` |
| **Sort (Descending)** | `Sort.Descending(x => x.Age)` | `Sort.Descending("Age")` |
| **Project (Expression)** | `Projection.Expression(x => new { x.Name })` | `Projection.Include("Name")` |
| **Index (Ascending)** | `IndexKeys.Ascending(x => x.Email)` | `IndexKeys.Ascending("Email")` |
| **Index (Text)** | `IndexKeys.Text(x => x.Name)` | `IndexKeys.Text("Name")` |
| **Lookup (Join)** | `Lookup(..., u => u.Id, o => o.UserId, ...)` | `Lookup("users", "_id", "UserId", ...)` |
| **Unwind** | `Unwind<T, TResult>(x => x.Array)` | `Unwind("Array")` |
| **LINQ** | `AsQueryable().Where(x => x.IsActive)` | N/A (magic strings not applicable) |
| **Aggregation Match** | `Aggregate().Match(x => x.IsActive)` | `Aggregate().Match("{ IsActive: true }")` |

## Best Practices

1. **Always use lambda expressions** for type safety and IDE support (IntelliSense, refactoring).
2. **Prefer `AsQueryable()` for simple LINQ queries** - it translates to MongoDB query language.
3. **Use aggregation pipeline for complex operations** - Lookup, Facet, Group, Unwind.
4. **Create strongly-typed indexes** using `Builders<T>.IndexKeys` with lambda expressions.
5. **Use `BulkWriteAsync`** for batch operations to reduce round trips.
6. **Leverage projection** to reduce network payload - only select fields you need.
7. **Chain update operations** with `Builders<T>.Update.Set().Set().Inc()` for atomic updates.
8. **For arrayFilters with In/Nin** - use custom extension methods (ArrayFilterIn/ArrayFilterNin) to maintain type safety instead of manually constructing BsonDocument.

## Notes

- This file focuses on strongly-typed patterns for MongoDB.Driver 3.x.
- Avoid string-based field references for maintainability and compile-time safety.
- For detailed rationale and compatibility notes, see `BEST-PRACTICES.md` (if created).
