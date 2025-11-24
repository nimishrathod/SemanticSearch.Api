using Microsoft.SemanticKernel.Embeddings;
using Microsoft.SemanticKernel;

namespace SemanticSearch.Api.Services;

public class EmbeddingService(Kernel kernel)
{
    private readonly ITextEmbeddingGenerationService _embeddingGenerator = kernel.GetRequiredService<ITextEmbeddingGenerationService>();

    public async Task<ReadOnlyMemory<float>> GenerateEmbeddingAsync(string text)
    {
        var embedding = await _embeddingGenerator.GenerateEmbeddingAsync(text);
        return embedding;
    }

    public async Task<List<ReadOnlyMemory<float>>> GenerateBatchEmbeddingsAsync(List<string> texts)
    {
        var embeddings = await _embeddingGenerator.GenerateEmbeddingsAsync(texts);
        return [.. embeddings];
    }
}