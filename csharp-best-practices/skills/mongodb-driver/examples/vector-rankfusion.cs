// MongoDB C# Driver v3.9.0 — Vector Search, RankFusion & ScoreFusion
// Demonstrates: $vectorSearch, $rankFusion (3 overloads), $scoreFusion (v3.9.0),
//   $rerank (v3.8.0), Atlas Search Indexes (create/list/update/drop)

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
            // NestedFilter = ...,               // v3.8.0: filter on nested docs during vector search
            // ReturnStoredSource = true,         // v3.8.0: return stored fields from index
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

// --- ScoreFusion (v3.9.0 — score-based combination, unlike rank-based RankFusion) ---

var scoreFusion = await collection.Aggregate()
    .ScoreFusion<Product, Product>(
        pipelines: new Dictionary<string, PipelineDefinition<Product, Product>>
        {
            ["text_search"]   = textPipeline,
            ["vector_search"] = vectorPipeline,
        },
        normalization: ScoreFusionNormalization.Sum,
        weights: new Dictionary<string, double>
        {
            ["text_search"]   = 0.3,
            ["vector_search"] = 0.7,
        })
    .ToListAsync();

// --- Rerank (v3.8.0 — re-score top results using Atlas cross-encoder model) ---

var reranked = await collection.Aggregate()
    .VectorSearch(
        field: "embedding",
        queryVector: QueryVector.Create(embedding),
        limit: 50,
        options: new VectorSearchOptions<Product> { IndexName = "my_vector_index" })
    .AppendStage(PipelineStageDefinitionBuilder.Rerank<Product, string>(
        query: new RerankQuery("wireless headphones"),
        path: p => p.Description,
        numDocsToRerank: 20,
        model: "my-reranker-model"))
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

// Create vector search index with stored fields (v3.8.0)
await collection.SearchIndexes.CreateOneAsync(
    new CreateVectorSearchIndexModel<Product>(
        field: p => p.Embedding,
        name: "my_vector_stored_index",
        dimensions: 1536,
        similarity: VectorSimilarity.Cosine,
        filterFields: p => p.Category)
    .WithIncludedStoredFields(p => p.Name, p => p.Description));

// Create auto-embedding vector index (Atlas)
await collection.SearchIndexes.CreateOneAsync(
    new CreateAutoEmbeddingVectorSearchIndexModel<Product>(
        field: p => p.Description,
        name: "my_auto_embed_index",
        embeddingModelName: "voyage-4"));

// List search indexes
using var cursor = await collection.SearchIndexes.ListAsync();
var indexes = await cursor.ToListAsync();

// --- Wait for search index READY (required after CreateOneAsync) ---

async Task WaitForSearchIndexReadyAsync(
    IMongoCollection<Product> coll, string indexName, TimeSpan? timeout = null)
{
    var deadline = TimeSpan.FromMinutes(2);
    var sw = System.Diagnostics.Stopwatch.StartNew();

    while (sw.Elapsed < (timeout ?? deadline))
    {
        using var idxCursor = await coll.SearchIndexes.ListAsync();
        var idxList = await idxCursor.ToListAsync();
        var idx = idxList.FirstOrDefault(x => x["name"].AsString == indexName);

        if (idx is not null)
        {
            var status = idx.GetValue("status", "UNKNOWN").AsString;
            if (status == "READY") return;
            if (status == "FAILED")
                throw new InvalidOperationException($"Search index creation failed: {idx}");
        }

        await Task.Delay(TimeSpan.FromSeconds(5));
    }

    throw new TimeoutException($"Search index '{indexName}' not READY within timeout");
}

// Usage: create index then wait
await collection.SearchIndexes.CreateOneAsync(
    new CreateSearchIndexModel(
        "my_new_index",
        new BsonDocument { { "mappings", new BsonDocument("dynamic", true) } }));
await WaitForSearchIndexReadyAsync(collection, "my_new_index");

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
