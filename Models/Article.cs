namespace SemanticSearch.Api.Models;

public record Article(
    string Id,
    string Title,
    string Content,
    string Category,
    string Date,
    string Url
);

public record SearchResult(
    string Title,
    string Url,
    string Category,
    string Score,
    string Snippet
);