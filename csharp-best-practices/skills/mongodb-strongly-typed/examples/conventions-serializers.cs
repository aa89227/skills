// MongoDB C# Driver — Conventions & Custom Serializers
// Demonstrates: CamelCase convention, CamelCase enum serializer,
//   strongly-typed ID with custom BsonSerializer

using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Bson.Serialization.Serializers;

// ========================================
// CONVENTION SETUP (run once at startup)
// ========================================

public static class MongoConventionSetup
{
    public static void RegisterConventions()
    {
        var camelCasePack = new ConventionPack
        {
            new CamelCaseElementNameConvention()    // PascalCase → camelCase field names
        };
        ConventionRegistry.Register("CamelCase", camelCasePack, _ => true);

        // Enums stored as camelCase strings (avoids magic numbers)
        BsonSerializer.RegisterSerializer(new CamelCaseEnumStringSerializer<UserTier>());
    }
}

// CamelCase enum serializer: UserTier.Premium → "premium"
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
        var name      = value.ToString();
        var camelCase = char.ToLowerInvariant(name[0]) + name[1..];
        context.Writer.WriteString(camelCase);
    }
}

// ========================================
// STRONGLY-TYPED IDs
// ========================================

public interface ValueObject;

// Strongly-typed UserId — wraps string ObjectId
public sealed record UserId(string Value) : ValueObject
{
    public static UserId New()              => new(ObjectId.GenerateNewId().ToString());
    public static UserId Parse(string value) => new(value);
    public override string ToString()       => Value;
}

public sealed record OrderId(string Value) : ValueObject;

// Custom BsonSerializer for UserId — handles both String and ObjectId BSON types
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
            BsonType.String   => new UserId(context.Reader.ReadString()),
            BsonType.ObjectId => new UserId(context.Reader.ReadObjectId().ToString()),
            _                 => throw new FormatException($"Cannot deserialize UserId from {type}")
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

// Register all serializers at startup (before any serialization occurs)
public static class BsonSerializerRegistration
{
    public static void Register()
    {
        BsonSerializer.RegisterSerializer(new UserIdBsonSerializer());
        // BsonSerializer.RegisterSerializer(new OrderIdBsonSerializer());
    }
}

// Use strongly-typed ID in document
public sealed class UserDocument
{
    [MongoDB.Bson.Serialization.Attributes.BsonId]
    public required UserId Id { get; init; }

    public required string Name { get; init; }
    public required List<OrderId> OrderIds { get; init; }
}

// Use in queries — fully type-safe
// var filter = Builders<UserDocument>.Filter.Eq(x => x.Id, userId);

public enum UserTier { Standard, Premium, VIP }
