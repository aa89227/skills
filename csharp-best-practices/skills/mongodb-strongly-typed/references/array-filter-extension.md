# MongoDB ArrayFilter Extension — In/Nin

MongoDB.Driver does not natively support strongly-typed `In`/`Nin` operators in `arrayFilters`.
This extension provides type-safe helpers that generate the correct `BsonDocument` filter.

## Extension Method

```csharp
using MongoDB.Bson;
using MongoDB.Driver;
using System.Linq.Expressions;

public static class ArrayFilterExtensions
{
    /// <summary>
    /// Creates an arrayFilter that matches array elements where a field is in the given values.
    /// Equivalent to: { "identifier.Field": { $in: [...values] } }
    /// </summary>
    public static BsonDocumentArrayFilterDefinition<TDocument> ArrayFilterIn<TDocument, TItem, TField>(
        this FilterDefinitionBuilder<TDocument> _,
        Expression<Func<TDocument, IEnumerable<TItem>>> arrayField,
        Expression<Func<TItem, TField>> itemField,
        IEnumerable<TField> values,
        string identifier)
    {
        var fieldName = GetMemberName(itemField);
        return new BsonDocumentArrayFilterDefinition<TDocument>(
            new BsonDocument($"{identifier}.{fieldName}",
                new BsonDocument("$in", new BsonArray(values.Cast<object>()))));
    }

    /// <summary>
    /// Creates an arrayFilter that matches array elements where a field is NOT in the given values.
    /// Equivalent to: { "identifier.Field": { $nin: [...values] } }
    /// </summary>
    public static BsonDocumentArrayFilterDefinition<TDocument> ArrayFilterNin<TDocument, TItem, TField>(
        this FilterDefinitionBuilder<TDocument> _,
        Expression<Func<TDocument, IEnumerable<TItem>>> arrayField,
        Expression<Func<TItem, TField>> itemField,
        IEnumerable<TField> values,
        string identifier)
    {
        var fieldName = GetMemberName(itemField);
        return new BsonDocumentArrayFilterDefinition<TDocument>(
            new BsonDocument($"{identifier}.{fieldName}",
                new BsonDocument("$nin", new BsonArray(values.Cast<object>()))));
    }

    private static string GetMemberName<TItem, TField>(Expression<Func<TItem, TField>> expr)
    {
        if (expr.Body is MemberExpression member)
            return member.Member.Name;
        throw new ArgumentException("Expression must be a member access.", nameof(expr));
    }
}
```

## Usage

```csharp
// Match array elements where Status is in the given values
var arrayFilter = Builders<UserWithItems>.Filter.ArrayFilterIn(
    x => x.Items,          // array field (for type resolution)
    item => item.Status,   // field on the array element
    statuses,              // values to match
    "elem");               // identifier used in AllMatchingElements("elem")

var options = new UpdateOptions { ArrayFilters = new[] { arrayFilter } };

await _usersWithItems.UpdateOneAsync(
    Builders<UserWithItems>.Filter.Eq(x => x.Id, userId),
    Builders<UserWithItems>.Update.Set(x => x.Items.AllMatchingElements("elem").Status, newStatus),
    options);
```

## Notes

- The first lambda parameter (`x => x.Items`) is only used for type inference — the array field path is not extracted from it.
- `identifier` must match the string passed to `AllMatchingElements(identifier)` in the update.
- Generated BSON: `{ "elem.Status": { $in: ["Active", "Pending"] } }`
