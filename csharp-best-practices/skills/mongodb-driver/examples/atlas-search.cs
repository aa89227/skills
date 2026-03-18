// MongoDB C# Driver v3.7.0 — Atlas Search ($search stage)
// Demonstrates: text, phrase, compound, autocomplete,
//   SearchOptions (index, score, highlight, pagination)

using MongoDB.Driver;
using MongoDB.Driver.Search;

// Assumes: collection is IMongoCollection<Product>
var searchDef = Builders<Product>.Search;

// --- Text search ---

var textResults = await collection.Aggregate()
    .Search(
        searchDef.Text(p => p.Description, "wireless headphones"),
        new SearchOptions<Product> { IndexName = "my_search_index" })
    .ToListAsync();

// --- Phrase search (exact ordered sequence) ---

var phraseResults = await collection.Aggregate()
    .Search(
        searchDef.Phrase(
            p => p.Description,
            "wireless headphones",
            slop: 2),                   // allowable word-distance between terms
        new SearchOptions<Product> { IndexName = "my_search_index" })
    .ToListAsync();

// Phrase with full options
var phraseResultsFull = await collection.Aggregate()
    .Search(
        searchDef.Phrase(
            p => p.Description,
            "wireless headphones",
            new SearchPhraseOptions<Product>
            {
                Slop     = 2,
                Synonyms = "my_synonym_mapping"
            }),
        new SearchOptions<Product> { IndexName = "my_search_index" })
    .ToListAsync();

// --- Compound (boolean: must / mustNot / should / filter) ---

var compoundResults = await collection.Aggregate()
    .Search(
        searchDef.Compound()
            .Must(searchDef.Text(p => p.Description, "headphones"))
            .MustNot(searchDef.Text(p => p.Description, "wired"))
            .Should(searchDef.Text(p => p.Brand, "Sony"))
            .Filter(searchDef.Equals(p => p.InStock, true))
            .MinimumShouldMatch(1),
        new SearchOptions<Product> { IndexName = "my_search_index" })
    .ToListAsync();

// --- Autocomplete ---

var autoResults = await collection.Aggregate()
    .Search(
        searchDef.Autocomplete(p => p.Name, "wire"),
        new SearchOptions<Product> { IndexName = "my_autocomplete_index" })
    .ToListAsync();

// --- SearchOptions (all options) ---

var searchOptions = new SearchOptions<Product>
{
    IndexName          = "my_search_index",
    ScoreDetails       = true,               // include score breakdown via $meta
    ReturnStoredSource = true,               // return stored fields (skip full doc lookup)
    CountOptions       = new SearchCountOptions
    {
        Type = SearchCountType.Total         // get total match count
    },
    Highlight = new SearchHighlightOptions<Product>
    {
        Path = p => p.Description           // highlight matching terms in this field
    },
    Sort       = Builders<Product>.Sort.Descending("score"),
    SearchAfter = "eyJ...",                  // cursor-based pagination token
};

// --- SearchMeta (facets + count without returning docs) ---

var meta = await collection.Aggregate()
    .SearchMeta(
        searchDef.Facet(
            searchDef.Text(p => p.Description, "headphones"),
            new SearchFacet<Product>[]
            {
                SearchFacet.String(p => p.Brand, "brand_facet", 5),
                SearchFacet.Number(p => p.Price, "price_facet",
                    new SearchFacetBound<double>[]
                    {
                        SearchFacetBound.Create(0.0),
                        SearchFacetBound.Create(100.0),
                        SearchFacetBound.Create(500.0),
                    })
            }),
        new SearchMetaOptions { IndexName = "my_search_index" })
    .SingleAsync();

// Supporting type
public class Product
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public string Brand { get; set; }
    public decimal Price { get; set; }
    public bool InStock { get; set; }
    public float[] Embedding { get; set; }
    public string Category { get; set; }
}
