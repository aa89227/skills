// MongoDB C# Driver v3.7.0 — Builders
// Demonstrates: Filter, Update, Sort, Projection builders

using MongoDB.Bson;
using MongoDB.Driver;

// --- Filter builders ---

var f = Builders<Person>.Filter;

// Comparison
var eq   = f.Eq(p => p.Name, "Alice");
var ne   = f.Ne(p => p.Name, "Alice");
var gt   = f.Gt(p => p.Age, 30);
var gte  = f.Gte(p => p.Age, 30);
var lt   = f.Lt(p => p.Age, 30);
var lte  = f.Lte(p => p.Age, 30);
var @in  = f.In(p => p.Status, new[] { "Active", "Pending" });
var nin  = f.Nin(p => p.Status, new[] { "Banned", "Deleted" });
var rx   = f.Regex(p => p.Name, new BsonRegularExpression("^A", "i"));

// Logical combination
var and  = f.And(f.Gte(p => p.Age, 18), f.Lt(p => p.Age, 65));
var or   = f.Or(f.Eq(p => p.Status, "A"), f.Eq(p => p.Status, "B"));
var not  = f.Not(f.Eq(p => p.Status, "Inactive"));

// Array operators
var anyEq     = f.AnyEq(p => p.Tags, "admin");
var size      = f.Size(p => p.Tags, 3);
var elemMatch = f.ElemMatch(p => p.Tags, t => t == "admin");

// Existence / Type
var exists = f.Exists(p => p.NickName);
var type   = f.Type(p => p.Age, BsonType.Int32);

// Matches all
var empty  = f.Empty;

// --- Update builders ---

var u = Builders<Person>.Update;

var setField    = u.Set(p => p.Name, "New Name");
var unset       = u.Unset(p => p.NickName);
var inc         = u.Inc(p => p.Score, 5);
var mul         = u.Mul(p => p.Score, 2);
var min         = u.Min(p => p.Score, 0);
var max         = u.Max(p => p.Score, 100);
var curDate     = u.CurrentDate(p => p.UpdatedAt);
var rename      = u.Rename("old_field", "new_field");
var setOnInsert = u.SetOnInsert(p => p.CreatedAt, DateTime.UtcNow);

// Array update operators
var push     = u.Push(p => p.Tags, "newTag");
var pushEach = u.PushEach(p => p.Tags, new[] { "a", "b" });
var addToSet = u.AddToSet(p => p.Tags, "unique");
var pull     = u.Pull(p => p.Tags, "removeMe");
var popFirst = u.PopFirst(p => p.Tags);
var popLast  = u.PopLast(p => p.Tags);

// Combine multiple updates
var combined = u.Combine(
    u.Set(p => p.Name, "Updated"),
    u.Inc(p => p.Score, 10));

// --- Sort builders ---

var s = Builders<Person>.Sort;

var ascending  = s.Ascending(p => p.Name);
var descending = s.Descending(p => p.Age);
var multiSort  = s.Combine(s.Ascending(p => p.Name), s.Descending(p => p.Age));

// --- Projection builders ---

var proj = Builders<Person>.Projection;

// Include specific fields
var include = proj.Include(p => p.Name).Include(p => p.Age);

// Exclude specific field
var exclude = proj.Exclude(p => p.Email);

// Use with Find (returns BsonDocument)
var bsonResult = await collection.Find(_ => true)
    .Project<BsonDocument>(include)
    .ToListAsync();

// Expression projection (returns anonymous type, no projection builder needed)
var anonResult = await collection.Find(_ => true)
    .Project(p => new { p.Name, p.Age })
    .ToListAsync();

// Supporting type (referenced by examples above)
public class Person
{
    public string Id { get; set; }
    public string Name { get; set; }
    public int Age { get; set; }
    public string Status { get; set; }
    public string? NickName { get; set; }
    public int Score { get; set; }
    public List<string> Tags { get; set; } = [];
    public DateTime UpdatedAt { get; set; }
    public DateTime CreatedAt { get; set; }
}
