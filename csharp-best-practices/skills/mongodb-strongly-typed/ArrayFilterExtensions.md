# ArrayFilter Extension (In/Nin)

MongoDB.Driver doesn't natively support strongly-typed In/Nin operators in arrayFilters. Below is a custom extension method solution.

## Extension Implementation

```csharp
using System.Linq.Expressions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

//Extension methods for strongly-typed arrayFilters with In/Nin operators.
public static class ArrayFilterBuilderExtensions
{
    extension<TDocument>(FilterDefinitionBuilder<TDocument> _)
    {
        //Create arrayFilter with In operator (elem.Field in [values]).
        public ArrayFilterDefinition<TDocument> ArrayFilterIn<TElement, TField>(
            Expression<Func<TDocument, IEnumerable<TElement>>> arraySelector,
            Expression<Func<TElement, TField>> fieldSelector,
            IEnumerable<TField> values,
            string identifier)
        {
            var elemFilterDef = Builders<TElement>.Filter.In(fieldSelector, values);

            var serializer = BsonSerializer.LookupSerializer<TElement>();
            var registry = BsonSerializer.SerializerRegistry;

            var rendered = elemFilterDef
                .Render(new RenderArgs<TElement>(serializer, registry))
                .AsBsonDocument;

            var arrayFilterDoc = new BsonDocument();
            foreach (var e in rendered.Elements)
                arrayFilterDoc.Add($"{identifier}.{e.Name}", e.Value);

            return new BsonDocumentArrayFilterDefinition<TDocument>(arrayFilterDoc);
        }

        //Create arrayFilter with Nin operator (elem.Field not in [values]).
        public ArrayFilterDefinition<TDocument> ArrayFilterNin<TElement, TField>(
            Expression<Func<TDocument, IEnumerable<TElement>>> arraySelector,
            Expression<Func<TElement, TField>> fieldSelector,
            IEnumerable<TField> values,
            string identifier)
        {
            var elemFilterDef = Builders<TElement>.Filter.Nin(fieldSelector, values);

            var serializer = BsonSerializer.LookupSerializer<TElement>();
            var registry = BsonSerializer.SerializerRegistry;

            var rendered = elemFilterDef
                .Render(new RenderArgs<TElement>(serializer, registry))
                .AsBsonDocument;

            var arrayFilterDoc = new BsonDocument();
            foreach (var e in rendered.Elements)
                arrayFilterDoc.Add($"{identifier}.{e.Name}", e.Value);

            return new BsonDocumentArrayFilterDefinition<TDocument>(arrayFilterDoc);
        }
    }
}
```

## Usage Example

See `UpdateItemsByStatusAsync` method in SKILL.md for how to use these extensions.

## How It Works

1. Use `Builders<TElement>.Filter.In/Nin` to create element-level filter
2. Render the filter to BsonDocument via `Render`
3. Prefix field names with `identifier.` (e.g., `elem.Status`)
4. Return `BsonDocumentArrayFilterDefinition` for use in UpdateOptions

This maintains type safety while implementing MongoDB arrayFilters In/Nin operations.
