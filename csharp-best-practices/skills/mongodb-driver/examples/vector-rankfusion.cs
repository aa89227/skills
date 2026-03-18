// MongoDB C# Driver v3.7.0 — Vector Search & RankFusion
// Demonstrates: $vectorSearch, $rankFusion (3 overloads),
//   Atlas Search Indexes (create/list/update/drop)

using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Search;

// Assumes: collection is IMongoCollection<Product>
// Assumes: GetEmbeddingAsync() returns float[] from an embedding model

float[] embedding = await GetEmbeddingAsync("wireless headphones");

// --- Vector Search ($vectorSearch) ---

var vectorResults = await collection.Aggregate()
    .VectorSearch(
        field: "embedding",
        queryVector: QueryVector.Create(embedding),
        limit: 10,
        options: new VectorSearchOptions<Product>
        {
            IndexName          = "my_vector_index",
            NumberOfCandidates = 100,            // ANN candidate pool (higher = more accurate, slower)
            Filter = Builders<Product>.Filter.Eq(p => p.Category, "electronics"),
            // Exact = true,                     // ENN: exact nearest-neighbor (slower)
            // AutoEmbeddingModelName = "voyage-4", // Atlas auto-embedding
        })
    .Project(Builders<Product>.Projection.Include(p => p.Name).Include(p => p.Description))
    .ToListAsync();

// --- Build sub-pipelines for RankFusion ---

var textPipeline = PipelineDefinition<Product, Product>.Create(new[]
{
    PipelineStageDefinitionBuilder.Search(
        Builders<Product>.Search.Phrase(p => p.Description, "wireless headphones"),
        new SearchOptions<Product> { IndexName = "my_search_index" })
});

var vectorPipeline = PipelineDefinition<Product, Product>.Create(new[]
{
    PipelineStageDefinitionBuilder.VectorSearch(
        (FieldDefinition<Product>)"embedding",
        QueryVector.Create(embedding),
        limit: 10,
        new VectorSearchOptions<Product> { IndexName = "my_vector_index" })
});

// --- RankFusion overload 1: Named pipelines + explicit weights ---

var rankFusion1 = await collection.Aggregate()
    .RankFusion<Product, Product>(
        pipelines: new Dictionary<string, PipelineDefinition<Product, Product>>
        {
            ["text_search"]   = textPipeline,
            ["vector_search"] = vectorPipeline,
        },
        weights: new Dictionary<string, double>
        {
            ["text_search"]   = 0.3,
            ["vector_search"] = 0.7,
        },
        options: new RankFusionOptions<Product> { ScoreDetails = true })
    .ToListAsync();

// --- RankFusion overload 2: Tuple array (auto-named pipeline1/pipeline2, per-pipeline weights) ---

var rankFusion2 = await collection.Aggregate()
    .RankFusion<Product, Product>(
        pipelinesWithWeights: new[]
        {
            (textPipeline,   (double?)0.3),
            (vectorPipeline, (double?)0.7),
        })
    .ToListAsync();

// --- RankFusion overload 3: Array of pipelines (auto-named, equal weights) ---

var rankFusion3 = await collection.Aggregate()
    .RankFusion<Product, Product>(
        pipelines: new[] { textPipeline, vectorPipeline })
    .ToListAsync();

// --- Atlas Search Indexes ---

// Create full-text search index
await collection.SearchIndexes.CreateOneAsync(
    new CreateSearchIndexModel(
        "my_search_index",
        new BsonDocument
        {
            { "mappings", new BsonDocument("dynamic", true) }
        }));

// Create vector search index (strongly-typed)
await collection.SearchIndexes.CreateOneAsync(
    new CreateVectorSearchIndexModel<Product>(
        field: p => p.Embedding,
        name: "my_vector_index",
        dimensions: 1536,
        similarity: VectorSimilarity.Cosine,
        filterFields: p => p.Category));

// Create auto-embedding vector index (Atlas)
await collection.SearchIndexes.CreateOneAsync(
    new CreateAutoEmbeddingVectorSearchIndexModel<Product>(
        field: p => p.Description,
        name: "my_auto_embed_index",
        embeddingModelName: "voyage-4"));

// List search indexes
using var cursor = await collection.SearchIndexes.ListAsync();
var indexes = await cursor.ToListAsync();

// Update search index
await collection.SearchIndexes.UpdateAsync(
    "my_search_index",
    new BsonDocument { { "mappings", new BsonDocument("dynamic", true) } });

// Drop search index
await collection.SearchIndexes.DropOneAsync("my_search_index");

// --- GridFS ---

using MongoDB.Driver.GridFS;

var bucket = new GridFSBucket(database, new GridFSBucketOptions
{
    BucketName     = "attachments",
    ChunkSizeBytes = 1048576  // 1 MB chunks
});

// Upload
using var uploadStream = File.OpenRead("report.pdf");
var fileId = await bucket.UploadFromStreamAsync("report.pdf", uploadStream);

// Download
using var downloadStream = File.Create("downloaded.pdf");
await bucket.DownloadToStreamAsync(fileId, downloadStream);

// Find files
using var fileCursor = await bucket.FindAsync(
    Builders<GridFSFileInfo>.Filter.Eq(f => f.Filename, "report.pdf"));
var files = await fileCursor.ToListAsync();

// Delete
await bucket.DeleteAsync(fileId);

// Supporting type
public class Product
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public string Brand { get; set; }
    public bool InStock { get; set; }
    public float[] Embedding { get; set; }
    public string Category { get; set; }
    public decimal Price { get; set; }
}
