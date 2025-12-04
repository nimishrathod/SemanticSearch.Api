using System.Numerics;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Embeddings;
using Qdrant.Client;
using SemanticSearch.Api.Models;
using SemanticSearch.Api.Services;


var builder = Host.CreateApplicationBuilder(args);

// Configure Semantic Kernel with Huggingface
builder.Services.AddSingleton(sp =>
{
    var kernel = Kernel.CreateBuilder()
                .AddHuggingFaceEmbeddingGenerator("sentence-transformers/all-MiniLM-L6-v2")
                .Build();
    return kernel;
});

// Configure Qdrant client
builder.Services.AddSingleton(sp => new QdrantClient("localhost", 6333));

// Register Services
builder.Services.AddSingleton<EmbeddingService>();
builder.Services.AddSingleton<VectorStoreService>();

var app = builder.Build();

// Load articles
var articles = JsonSerializer.Deserialize<List<Article>>(
    await File.ReadAllTextAsync("Data/articles.json")
) ?? [];

var vectorStore = app.Services.GetRequiredService<VectorStoreService>();

Console.WriteLine("=== Semantic Search POC ===\n");
Console.WriteLine("1. Indexing articles...");
await vectorStore.IndexArticlesAsync(articles);

// Interactive search
Console.WriteLine("\n2. Starting interactie search...");
Console.WriteLine("Type your search query (or 'quit' to exit):\n");

while (true)
{
    Console.WriteLine("Search: ");
    var query = Console.ReadLine();

    if (string.IsNullOrWhiteSpace(query) || query.ToLower() == "quit")
        break;

    Console.WriteLine($"\nSearching for: '{query}'...\n");

    var results = await vectorStore.SearchAsync(query, topK: 3);

    if (results.Count == 0)
    {
        Console.WriteLine("No results found.\n");
        continue;
    }

    foreach (var result in results)
    {
        Console.WriteLine($"📄 {result.Title}");
        Console.WriteLine($"    Score: {result.Score:F4}");
        Console.WriteLine($"    Category: {result.Category}");
        Console.WriteLine($"    URL: {result.Url}");
        Console.WriteLine($"    Preview: {result.Snippet}");
        Console.WriteLine();
    }
}

Console.WriteLine("Goodbye & Take Care!");