
using Microsoft.SemanticKernel.Connectors.Qdrant;
using Qdrant.Client;
using SemanticSearch.Api.Models;
using Microsoft.Extensions.VectorData;

namespace SemanticSearch.Api.Services;

public class VectorStoreService(QdrantClient qdrantClient, EmbeddingService embeddingService)
{
    private readonly QdrantVectorStore _vectorStore = new(qdrantClient, false);
    private readonly EmbeddingService _embeddingService = embeddingService;
    private const string CollectionName = "articles";

    public async Task IndexArticlesAsync(List<Article> articles)
    {
        var collection = _vectorStore.GetCollection<Guid, ArticleRecord>(CollectionName);

        await collection.EnsureCollectionExistsAsync();

        foreach (var article in articles)
        {
            var embedding = await _embeddingService.GenerateEmbeddingAsync(article.Content);

            var record = new ArticleRecord
            {
                Key = Guid.Parse(article.Id),
                Title = article.Title,
                Content = article.Content,
                Category = article.Category,
                Date = article.Date,
                Url = article.Url,
                Embedding = embedding
            };

            await collection.UpsertAsync(record);
        }

        Console.WriteLine($"Indexed {articles.Count} articles successfully!");
    }

    public async Task<List<SearchResult>> SearchAsync(string query, int topK = 5)
    {
        var collection = _vectorStore.GetCollection<Guid, ArticleRecord>(CollectionName);
        var queryEmbedding = await _embeddingService.GenerateEmbeddingAsync(query);

        var results = new List<SearchResult>();

        await foreach (var result in collection.SearchAsync(queryEmbedding, topK))
        {
            results.Add(new SearchResult(
                result.Record.Title,
                result.Record.Url,
                result.Record.Category,
                result.Score,
                result.Record.Content[..Math.Min(150, result.Record.Content.Length)] + "..."
            ));
        }

        return results;
    }
}

public class ArticleRecord
{
    [VectorStoreKey]
    public Guid Key { get; set; }
    [VectorStoreData]
    public string Title { get; set; } = string.Empty;
    [VectorStoreData]
    public string Content { get; set; } = string.Empty;
    [VectorStoreData]
    public string Category { get; set; } = string.Empty;
    [VectorStoreData]
    public string Date { get; set; } = string.Empty;
    [VectorStoreData]
    public string Url { get; set; } = string.Empty;
    [VectorStoreVector(384)] // For HuggingFace
    public ReadOnlyMemory<float> Embedding { get; set; }

}