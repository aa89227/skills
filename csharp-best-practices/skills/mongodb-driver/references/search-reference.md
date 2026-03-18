# MongoDB Atlas Search Reference — v3.7.0

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

## RankFusion Overloads

| Overload | Parameters | Use Case |
|---|---|---|
| Named Dictionary | `pipelines: Dictionary<string, Pipeline>`, `weights?: Dictionary<string, double>`, `options?` | Named pipelines with per-pipeline weights |
| Tuple Array | `pipelinesWithWeights: (Pipeline, double?)[]`, `options?` | Auto-named with per-pipeline weights |
| Array | `pipelines: Pipeline[]`, `options?` | Auto-named, equal weights |

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

## Notes

- `$search` and `$vectorSearch` are **separate aggregation stages** — cannot be used in the same pipeline directly; use `$rankFusion` to combine them.
- `RankFusion` is an **extension method** on `IAggregateFluent`, not a native method.
- Atlas Search indexes must be created via `collection.SearchIndexes`, not `collection.Indexes`.
- `SearchMeta` returns facet counts without returning matching documents.
- `ReturnStoredSource = true` requires the index to have stored fields configured.
