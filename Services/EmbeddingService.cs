using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SemanticSearch.Api.Services;

public class EmbeddingService
{
    private readonly HttpClient _httpClient = new();
    private const string HuggingFaceUrl = "https://api-inference.huggingface.co/pipeline/feature-extraction/sentence-transformers/all-MiniLM-L6-v2";
    private const string ModelName = "sentence-transformers/all-MiniLM-L6-v2";

    public async Task<ReadOnlyMemory<float>> GenerateEmbeddingAsync(string text)
    {
        var embeddings = await GenerateBatchEmbeddingsAsync([text]);
        return embeddings.FirstOrDefault();
    }

    public async Task<List<ReadOnlyMemory<float>>> GenerateBatchEmbeddingsAsync(List<string> texts)
    {
        try
        {
            var requestBody = new { inputs = texts };
            var content = new StringContent(
                JsonSerializer.Serialize(requestBody),
                Encoding.UTF8,
                "application/json"
            );

            // Try to use Hugging Face with Authorization header if token is available
            var hfToken = Environment.GetEnvironmentVariable("HF_TOKEN");
            if (!string.IsNullOrEmpty(hfToken))
            {
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {hfToken}");
            }

            var response = await _httpClient.PostAsync(HuggingFaceUrl, content);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Hugging Face API error: {response.StatusCode} - {errorContent}");

                // Fallback to local processing if API fails
                return GenerateLocalEmbeddings(texts);
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            var embeddings = JsonSerializer.Deserialize<float[][]>(responseContent);

            if (embeddings == null)
            {
                throw new Exception("Failed to parse embedding response from Hugging Face");
            }

            return embeddings
                .Select(e => new ReadOnlyMemory<float>(e))
                .ToList();
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"Network error calling Hugging Face API: {ex.Message}");
            Console.WriteLine("Falling back to local random embeddings for testing");
            return GenerateLocalEmbeddings(texts);
        }
    }

    private List<ReadOnlyMemory<float>> GenerateLocalEmbeddings(List<string> texts)
    {
        // Fallback: Generate random embeddings for testing
        // In production, you might want to use a local model or throw an error
        var random = new Random(42); // Deterministic for testing
        var embeddings = new List<ReadOnlyMemory<float>>();

        foreach (var text in texts)
        {
            var embedding = new float[384];
            for (int i = 0; i < embedding.Length; i++)
            {
                embedding[i] = (float)(random.NextDouble() - 0.5) * 2;
            }
            embeddings.Add(new ReadOnlyMemory<float>(embedding));
        }

        return embeddings;
    }
}