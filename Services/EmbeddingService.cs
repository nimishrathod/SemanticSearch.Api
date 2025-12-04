using Microsoft.SemanticKernel.Embeddings;
using Microsoft.SemanticKernel;

namespace SemanticSearch.Api.Services;

public class EmbeddingService(Kernel kernel)
{
    private readonly Kernel _kernel = kernel;

    public async Task<ReadOnlyMemory<float>> GenerateEmbeddingAsync(string text)
    {
        var embeddingGenerator = _kernel.GetRequiredService<ITextEmbeddingGenerationService>();
        var embedding = await embeddingGenerator.GenerateEmbeddingAsync(text);
        return embedding;
    }

    public async Task<List<ReadOnlyMemory<float>>> GenerateBatchEmbeddingsAsync(List<string> texts)
    {
        var embeddingGenerator = _kernel.GetRequiredService<ITextEmbeddingGenerationService>();
        var embeddings = await embeddingGenerator.GenerateEmbeddingsAsync(texts);
        return [.. embeddings];
    }
}