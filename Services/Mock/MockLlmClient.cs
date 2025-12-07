using RAG.Pipeline.Interfaces;

namespace RAG.Pipeline.Services.Mock;

/// <summary>
/// Mock LLM client for testing. Generates simple expansions and summaries.
/// Replace with actual implementation (OpenAI, Azure OpenAI, Anthropic, etc.).
/// </summary>
public class MockLlmClient : ILlmClient
{
    private readonly ILogger<MockLlmClient> _logger;

    public MockLlmClient(ILogger<MockLlmClient> logger)
    {
        _logger = logger;
    }

    public Task<IReadOnlyList<string>> GenerateQueryExpansionsAsync(string userQuery)
    {
        // Generate simple variations of the query
        var expansions = new List<string>
        {
            $"What is {userQuery}?",
            $"Explain {userQuery}",
            $"How does {userQuery} work?",
            $"Information about {userQuery}",
            $"Details on {userQuery}"
        };

        _logger.LogInformation("Generated {Count} query expansions", expansions.Count);
        return Task.FromResult<IReadOnlyList<string>>(expansions);
    }

    public Task<string> GenerateSummaryAsync(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return Task.FromResult(string.Empty);

        // Simple summary: take first 100 characters or first sentence
        var summary = text.Length > 100 
            ? text.Substring(0, 100).Trim() + "..." 
            : text.Trim();

        var firstPeriod = summary.IndexOf('.');
        if (firstPeriod > 0 && firstPeriod < 150)
        {
            summary = summary.Substring(0, firstPeriod + 1);
        }

        return Task.FromResult(summary);
    }

    public Task<IReadOnlyList<string>> ExtractKeywordsAsync(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return Task.FromResult<IReadOnlyList<string>>(Array.Empty<string>());

        // Simple keyword extraction: find words longer than 5 characters
        var words = text.Split(new[] { ' ', ',', '.', '!', '?', ';', ':', '\n', '\r' }, 
            StringSplitOptions.RemoveEmptyEntries);
        
        var keywords = words
            .Where(w => w.Length > 5)
            .Select(w => w.ToLowerInvariant())
            .Distinct()
            .Take(10)
            .ToList();

        return Task.FromResult<IReadOnlyList<string>>(keywords);
    }
}
