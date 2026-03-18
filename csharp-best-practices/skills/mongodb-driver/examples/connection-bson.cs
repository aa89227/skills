// MongoDB C# Driver v3.7.0 — Connection & BSON Model
// Demonstrates: MongoClient constructors, get database/collection,
//   typed document with attributes, BsonDocument (untyped)

using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;

// --- Connection ---

// Connection string
var client = new MongoClient("mongodb://localhost:27017");

// MongoClientSettings (full control)
var settings = MongoClientSettings.FromConnectionString("mongodb+srv://user:pass@cluster.mongodb.net/");
settings.ServerApi = new ServerApi(ServerApiVersion.V1);
settings.MaxConnectionPoolSize = 100;
var client2 = new MongoClient(settings);

// Get database and collection
var database = client.GetDatabase("mydb");
var collection = database.GetCollection<Person>("people");

// Untyped (BsonDocument)
var untypedCollection = database.GetCollection<BsonDocument>("people");

// --- Typed document model ---

public class Person
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; }

    [BsonElement("name")]
    public string Name { get; set; }

    [BsonElement("age")]
    public int Age { get; set; }

    [BsonIgnore]
    public string CalculatedField { get; set; }

    [BsonIgnoreIfNull]
    public string? NickName { get; set; }

    [BsonIgnoreIfDefault]
    public int Score { get; set; }

    [BsonRequired]
    public string Email { get; set; }

    [BsonExtraElements]
    public BsonDocument ExtraFields { get; set; }

    [BsonDefaultValue("Unknown")]
    public string Status { get; set; }

    public List<string> Tags { get; set; } = [];
}

// --- BsonDocument (untyped) ---

var doc = new BsonDocument
{
    { "name", "Alice" },
    { "age", 30 },
    { "tags", new BsonArray { "admin", "user" } }
};

// --- BsonClassMap (fluent mapping alternative to attributes) ---

using MongoDB.Bson.Serialization;

BsonClassMap.RegisterClassMap<Person>(cm =>
{
    cm.AutoMap();
    cm.SetIdMember(cm.GetMemberMap(c => c.Id));
    cm.MapMember(c => c.Name).SetElementName("name");
    cm.MapMember(c => c.Email).SetElementName("email").SetIsRequired(true);
    cm.MapMember(c => c.Age).SetElementName("age");
    cm.SetIgnoreExtraElements(true);
    cm.UnmapMember(c => c.CalculatedField);
});

// --- Conventions (apply globally, must run once at startup before serialization) ---

using MongoDB.Bson.Serialization.Conventions;

var conventionPack = new ConventionPack
{
    new CamelCaseElementNameConvention(),       // PascalCase → camelCase
    new IgnoreExtraElementsConvention(true),    // ignore unknown fields
    new IgnoreIfNullConvention(true),           // omit null fields
    new EnumRepresentationConvention(BsonType.String), // enums as strings
};

ConventionRegistry.Register(
    "MyAppConventions",
    conventionPack,
    t => t.Namespace?.StartsWith("MyApp.Models") == true);
