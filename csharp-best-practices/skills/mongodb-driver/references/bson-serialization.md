# MongoDB BSON Serialization Reference — v3.7.0

## BSON Attributes

| Attribute | Purpose |
|---|---|
| `[BsonId]` | Marks the `_id` field |
| `[BsonElement("name")]` | Maps property to BSON field name |
| `[BsonRepresentation(BsonType.ObjectId)]` | Stores `string` as `ObjectId` |
| `[BsonIgnore]` | Excludes property from serialization |
| `[BsonIgnoreIfNull]` | Omits field if value is null |
| `[BsonIgnoreIfDefault]` | Omits field if value is default |
| `[BsonRequired]` | Field must be present in BSON |
| `[BsonExtraElements]` | Captures unmapped fields into `BsonDocument` |
| `[BsonDefaultValue(val)]` | Default when field is missing in BSON |
| `[BsonDiscriminator("type")]` | Polymorphic type discriminator value |
| `[BsonKnownTypes(typeof(A), typeof(B))]` | Registers known derived types |
| `[BsonNoId]` | Document has no `_id` field |
| `[BsonConstructor]` | Marks constructor for deserialization |
| `[BsonSerializer(typeof(S))]` | Use custom serializer for this property |
| `[BsonDateTimeOptions(Kind = DateTimeKind.Utc)]` | DateTime serialization options |

## Common Conventions

| Convention | Effect |
|---|---|
| `CamelCaseElementNameConvention` | `FirstName` → `firstName` |
| `IgnoreExtraElementsConvention(true)` | Skip unmapped BSON fields |
| `IgnoreIfNullConvention(true)` | Omit null properties |
| `IgnoreIfDefaultConvention(true)` | Omit default-valued properties |
| `EnumRepresentationConvention(BsonType.String)` | Store enums as string |
| `ImmutableTypeClassMapConvention` | Support immutable types (records) |
| `NoIdMemberConvention` | Exclude `_id` from certain types |

## Convention Registration Pattern

```csharp
// Must run ONCE at startup, before any serialization
ConventionRegistry.Register(
    name: "MyAppConventions",
    conventions: new ConventionPack { ... },
    filter: t => t.Namespace?.StartsWith("MyApp.Models") == true);
```

## BsonClassMap vs Attributes

- **Attributes**: Declarative, inline with model. Preferred for simple cases.
- **BsonClassMap**: Programmatic, separate from model. Use when:
  - You can't modify the model class (external library)
  - You need conditional mapping logic
  - You want to keep domain models clean of persistence concerns

```csharp
BsonClassMap.RegisterClassMap<Person>(cm =>
{
    cm.AutoMap();                                          // scan properties
    cm.SetIdMember(cm.GetMemberMap(c => c.Id));            // set _id
    cm.MapMember(c => c.Name).SetElementName("name");      // rename field
    cm.MapMember(c => c.Email).SetIsRequired(true);        // required
    cm.SetIgnoreExtraElements(true);                       // ignore unknowns
    cm.UnmapMember(c => c.CalculatedField);                // exclude
    cm.SetDiscriminator("person");                         // polymorphism
    cm.AddKnownType(typeof(AdminPerson));                   // subtype
});
```

## Polymorphism

```csharp
[BsonDiscriminator("animal", Required = true)]
[BsonKnownTypes(typeof(Dog), typeof(Cat))]
public abstract class Animal
{
    public string Name { get; set; }
}

[BsonDiscriminator("dog")]
public class Dog : Animal { public string Breed { get; set; } }

[BsonDiscriminator("cat")]
public class Cat : Animal { public bool IsIndoor { get; set; } }
```
