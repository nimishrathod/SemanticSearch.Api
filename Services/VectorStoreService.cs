using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using SemanticSearch.Api.Models;

namespace SemanticSearch.Api.Services;

public class VectorStoreService(EmbeddingService embeddingService)
{
    private readonly EmbeddingService _embeddingService = embeddingService;
    private readonly HttpClient _httpClient = new();
    private const string QdrantUrl = "http://localhost:6333";
    private const string CollectionName = "articles";

    public async Task IndexArticlesAsync(List<Article> articles)
    {
        try
        {
            // Delete collection if exists
            try
            {
                await _httpClient.DeleteAsync($"{QdrantUrl}/collections/{CollectionName}");
            }
            catch
            {
                // Collection might not exist
            }

            // Create collection
            var createCollectionBody = new
            {
                vectors = new
                {
                    size = 384,
                    distance = "Cosine"
                }
            };

            Console.WriteLine($"Creating collection at: {QdrantUrl}/collections/{CollectionName}");
            Console.WriteLine($"Request body: {JsonSerializer.Serialize(createCollectionBody)}");

            var createResponse = await _httpClient.PutAsync(
                $"{QdrantUrl}/collections/{CollectionName}",
                new StringContent(JsonSerializer.Serialize(createCollectionBody), Encoding.UTF8, "application/json")
            );

            Console.WriteLine($"Create response status: {createResponse.StatusCode}");

            if (!createResponse.IsSuccessStatusCode)
            {
                var errorContent = await createResponse.Content.ReadAsStringAsync();
                Console.WriteLine($"Error response: {errorContent}");
                throw new Exception($"Failed to create collection: Status={createResponse.StatusCode}, Response={errorContent}");
            }            // Upsert points
            var points = new List<object>();
            int pointId = 1;

            foreach (var article in articles)
            {
                var embedding = await _embeddingService.GenerateEmbeddingAsync(article.Content);
                var embeddingList = embedding.ToArray().Select(f => (double)f).ToList();

                var point = new
                {
                    id = pointId++,
                    vector = embeddingList,
                    payload = new
                    {
                        title = article.Title,
                        content = article.Content,
                        category = article.Category,
                        date = article.Date,
                        url = article.Url
                    }
                };
                points.Add(point);
            }

            var upsertBody = new { points };
            var upsertResponse = await _httpClient.PutAsync(
                $"{QdrantUrl}/collections/{CollectionName}/points",
                new StringContent(JsonSerializer.Serialize(upsertBody), Encoding.UTF8, "application/json")
            );

            if (!upsertResponse.IsSuccessStatusCode)
            {
                var errorContent = await upsertResponse.Content.ReadAsStringAsync();
                throw new Exception($"Failed to upsert points: {errorContent}");
            }

            Console.WriteLine($"Indexed {articles.Count} articles successfully!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in IndexArticlesAsync: {ex.Message}");
            throw;
        }
    }

    public async Task<List<SearchResult>> SearchAsync(string query, int topK = 5)
    {
        var queryEmbedding = await _embeddingService.GenerateEmbeddingAsync(query);
        var embeddingList = queryEmbedding.ToArray().Select(f => (double)f).ToList();

        var searchBody = new
        {
            vector = embeddingList,
            limit = topK,
            with_payload = true
        };

        var searchResponse = await _httpClient.PostAsync(
            $"{QdrantUrl}/collections/{CollectionName}/points/search",
            new StringContent(JsonSerializer.Serialize(searchBody), Encoding.UTF8, "application/json")
        );

        var responseContent = await searchResponse.Content.ReadAsStringAsync();

        if (!searchResponse.IsSuccessStatusCode)
        {
            throw new Exception($"Search failed: {responseContent}");
        }

        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        var searchResultJson = JsonSerializer.Deserialize<SearchResponseWrapper>(responseContent, jsonOptions);
        var results = new List<SearchResult>();

        if (searchResultJson?.Result != null)
        {
            foreach (var item in searchResultJson.Result)
            {
                var payload = item.Payload;
                results.Add(new SearchResult(
                    payload?.Title ?? "Unknown",
                    payload?.Url ?? "",
                    payload?.Category ?? "",
                    item.Score,
                    (payload?.Content ?? "")[..Math.Min(150, (payload?.Content ?? "").Length)] + "..."
                ));
            }
        }

        return results;
    }
}

public class SearchResponseWrapper
{
    [JsonPropertyName("result")]
    public List<SearchItem>? Result { get; set; }
}

public class SearchItem
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("score")]
    public float Score { get; set; }

    [JsonPropertyName("payload")]
    public PayloadData? Payload { get; set; }
}

public class PayloadData
{
    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("content")]
    public string? Content { get; set; }

    [JsonPropertyName("category")]
    public string? Category { get; set; }

    [JsonPropertyName("date")]
    public string? Date { get; set; }

    [JsonPropertyName("url")]
    public string? Url { get; set; }
}