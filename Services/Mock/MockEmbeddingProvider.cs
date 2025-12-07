using RAG.Pipeline.Interfaces;

namespace RAG.Pipeline.Services.Mock;

/// <summary>
/// Mock embedding provider for testing. Generates random embeddings.
/// Replace with actual implementation (OpenAI, Azure OpenAI, BGE, E5, etc.).
/// </summary>
public class MockEmbeddingProvider : IEmbeddingProvider
{
    private const int EmbeddingDimension = 384; // Common dimension for sentence transformers
    private readonly Random _random = new();
    private readonly ILogger<MockEmbeddingProvider> _logger;

    public MockEmbeddingProvider(ILogger<MockEmbeddingProvider> logger)
    {
        _logger = logger;
    }

    public Task<float[]> EmbedAsync(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return Task.FromResult(Array.Empty<float>());

        // Generate a deterministic embedding based on text hash for consistency
        var hash = text.GetHashCode();
        var random = new Random(hash);
        
        var embedding = new float[EmbeddingDimension];
        for (int i = 0; i < EmbeddingDimension; i++)
        {
            embedding[i] = (float)(random.NextDouble() * 2 - 1); // Range [-1, 1]
        }

        // Normalize the vector
        var magnitude = Math.Sqrt(embedding.Sum(x => x * x));
        for (int i = 0; i < embedding.Length; i++)
        {
            embedding[i] /= (float)magnitude;
        }

        return Task.FromResult(embedding);
    }

    public async Task<float[][]> EmbedBatchAsync(IEnumerable<string> texts)
    {
        var embeddings = new List<float[]>();
        
        foreach (var text in texts)
        {
            embeddings.Add(await EmbedAsync(text));
        }

        return embeddings.ToArray();
    }
}
