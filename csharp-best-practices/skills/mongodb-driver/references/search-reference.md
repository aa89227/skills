# MongoDB Atlas Search Reference — v3.9.0

## Search Operators

| Operator | Builder Method | Description |
|---|---|---|
| `text` | `.Text(path, query)` | Full-text search |
| `phrase` | `.Phrase(path, query, slop?)` | Exact phrase with optional word-distance tolerance |
| `compound` | `.Compound().Must().Should().Filter()...` | Boolean combination |
| `autocomplete` | `.Autocomplete(path, query)` | Prefix/edge-ngram completion |
| `equals` | `.Equals(path, value)` | Exact value match |
| `exists` | `.Exists(path)` | Field existence check |
| `range` | `.Range(path, SearchRange)` | Numeric/date range |
| `regex` | `.Regex(path, pattern)` | Regular expression |
| `wildcard` | `.Wildcard(path, pattern)` | Wildcard pattern (`*`, `?`) |
| `near` | `.Near(path, origin, pivot)` | Proximity scoring (numeric/date/geo) |
| `moreLikeThis` | `.MoreLikeThis(docs)` | Similar documents |
| `queryString` | `.QueryString(defaultPath, query)` | Lucene-style query string |
| `span` | `.Span(spanDef)` | Positional term matching |
| `geoShape` | `.GeoShape(path, relation, geometry)` | Geospatial shape queries |
| `geoWithin` | `.GeoWithin(path, area)` | Geospatial containment |
| `embeddedDocument` | `.EmbeddedDocument(path, op)` | Search within nested arrays |
| `facet` | `.Facet(op, facets)` | Faceted search (use with `SearchMeta`) |

## SearchOptions

| Property | Type | Description |
|---|---|---|
| `IndexName` | `string` | Atlas Search index name (**required**) |
| `ScoreDetails` | `bool` | Include score breakdown in `$meta` |
| `ReturnStoredSource` | `bool` | Return stored fields, skip full doc lookup |
| `CountOptions` | `SearchCountOptions` | Get total match count (`Total` or `LowerBound`) |
| `Highlight` | `SearchHighlightOptions<T>` | Highlight matching terms in specified path |
| `Sort` | `SortDefinition<T>` | Sort results (e.g., by score or field) |
| `SearchAfter` | `string` | Cursor-based pagination token |

## VectorSearchOptions

| Property | Description |
|---|---|
| `IndexName` | Name of the vector search index |
| `NumberOfCandidates` | ANN candidate pool size (higher = more accurate, slower) |
| `Filter` | Pre-filter with `FilterDefinition<T>` |
| `Exact` | `true` for exact NN (ENN), `false` for ANN (default) |
| `AutoEmbeddingModelName` | Model name for Atlas auto-embedding |
| `NestedFilter` | Filter on nested documents during vector search (v3.8.0) |
| `ReturnStoredSource` | Return stored source fields from Atlas Search (v3.8.0) |
| `EmbeddedScoreMode` | Scoring function for embedded document results (v3.8.0) |

## RankFusion Overloads

| Overload | Parameters | Use Case |
|---|---|---|
| Named Dictionary | `pipelines: Dictionary<string, Pipeline>`, `weights?: Dictionary<string, double>`, `options?` | Named pipelines with per-pipeline weights |
| Tuple Array | `pipelinesWithWeights: (Pipeline, double?)[]`, `options?` | Auto-named with per-pipeline weights |
| Array | `pipelines: Pipeline[]`, `options?` | Auto-named, equal weights |

## ScoreFusion (v3.9.0)

Like `$rankFusion` but uses score-based combination with configurable normalization instead of rank-based reciprocal fusion.

| Overload | Parameters | Use Case |
|---|---|---|
| Named Dictionary | `pipelines: Dictionary<string, Pipeline>`, `normalization`, `weights?`, `options?` | Named pipelines with per-pipeline weights |
| Tuple Array | `pipelinesWithWeights: (Pipeline, double?)[]`, `normalization`, `options?` | Auto-named with per-pipeline weights |
| Array | `pipelines: Pipeline[]`, `normalization`, `options?` | Auto-named, equal weights |

`ScoreFusionNormalization` values: `Sum`, `None`.

## Rerank (v3.8.0)

Re-scores results using an Atlas model (e.g., cross-encoder reranker).

```
PipelineStageDefinitionBuilder.Rerank(query, path, numDocsToRerank, model)
```

| Parameter | Type | Description |
|---|---|---|
| `query` | `RerankQuery` | The query to rerank against |
| `path` | `Expression<Func<T, TField>>` | Field(s) to rerank on |
| `numDocsToRerank` | `int` | Number of top docs to send to the model |
| `model` | `string` | Atlas reranker model name |

## Atlas Search Index Types

| Model | Purpose |
|---|---|
| `CreateSearchIndexModel` | Full-text search index (BsonDocument mappings) |
| `CreateVectorSearchIndexModel<T>` | Vector search index (typed, with similarity + filterFields) |
| `CreateAutoEmbeddingVectorSearchIndexModel<T>` | Auto-embedding vector index (Atlas only) |

### Vector Similarity Options

| Value | Description |
|---|---|
| `VectorSimilarity.Cosine` | Cosine similarity (normalized vectors) |
| `VectorSimilarity.Euclidean` | Euclidean distance |
| `VectorSimilarity.DotProduct` | Dot product (unit-normalized vectors) |

### Vector Indexing Method (v3.9.0)

| Value | Description |
|---|---|
| `VectorIndexingMethod.Hnsw` | Hierarchical navigable small world (default) |
| `VectorIndexingMethod.Flat` | Exact brute-force search (slower, more accurate) |

### Vector Index Stored Fields (v3.8.0)

```csharp
new CreateVectorSearchIndexModel<T>(...)
    .WithIncludedStoredFields(x => x.Name, x => x.Description)
    // or
    .WithExcludedStoredFields(x => x.LargeBlob)
```

## Notes

- `$search` and `$vectorSearch` are **separate aggregation stages** — cannot be used in the same pipeline directly; use `$rankFusion` to combine them.
- `RankFusion` is an **extension method** on `IAggregateFluent`, not a native method.
- Atlas Search indexes must be created via `collection.SearchIndexes`, not `collection.Indexes`.
- `SearchMeta` returns facet counts without returning matching documents.
- `ReturnStoredSource = true` requires the index to have stored fields configured.
