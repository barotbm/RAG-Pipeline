using RAG.Pipeline.Interfaces;
using RAG.Pipeline.Models;
using System.Collections.Concurrent;

namespace RAG.Pipeline.Services;

/// <summary>
/// In-memory implementation of vector store using cosine similarity for search.
/// </summary>
public class InMemoryVectorStore : IVectorStore
{
    private readonly ConcurrentDictionary<string, DocumentChunk> _chunks = new();
    private readonly ILogger<InMemoryVectorStore> _logger;

    public InMemoryVectorStore(ILogger<InMemoryVectorStore> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc/>
    public Task IndexChunksAsync(IEnumerable<DocumentChunk> chunks)
    {
        foreach (var chunk in chunks)
        {
            _chunks[chunk.Id] = chunk;
        }

        _logger.LogInformation("Indexed {Count} chunks in vector store", _chunks.Count);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task<IReadOnlyList<ScoredChunk>> SearchByEmbeddingAsync(float[] queryEmbedding, int topK)
    {
        if (queryEmbedding == null || queryEmbedding.Length == 0)
        {
            return Task.FromResult<IReadOnlyList<ScoredChunk>>(Array.Empty<ScoredChunk>());
        }

        var scoredChunks = new List<ScoredChunk>();

        foreach (var chunk in _chunks.Values)
        {
            // Calculate similarity scores for all embedding types and take the maximum
            var scores = new List<double>();

            if (chunk.ContentEmbedding.Length > 0)
                scores.Add(CosineSimilarity(queryEmbedding, chunk.ContentEmbedding));

            if (chunk.TitleEmbedding.Length > 0)
                scores.Add(CosineSimilarity(queryEmbedding, chunk.TitleEmbedding));

            if (chunk.KeywordEmbedding.Length > 0)
                scores.Add(CosineSimilarity(queryEmbedding, chunk.KeywordEmbedding));

            if (chunk.SummaryEmbedding.Length > 0)
                scores.Add(CosineSimilarity(queryEmbedding, chunk.SummaryEmbedding));

            if (scores.Any())
            {
                // Use the maximum similarity across all embedding types
                var maxScore = scores.Max();
                scoredChunks.Add(new ScoredChunk { Chunk = chunk, Score = maxScore });
            }
        }

        var results = scoredChunks
            .OrderByDescending(sc => sc.Score)
            .Take(topK)
            .ToList();

        _logger.LogInformation("Vector search returned {Count} results", results.Count);
        return Task.FromResult<IReadOnlyList<ScoredChunk>>(results);
    }

    /// <summary>
    /// Calculates cosine similarity between two vectors.
    /// </summary>
    private static double CosineSimilarity(float[] vectorA, float[] vectorB)
    {
        if (vectorA.Length != vectorB.Length)
            return 0.0;

        double dotProduct = 0.0;
        double magnitudeA = 0.0;
        double magnitudeB = 0.0;

        for (int i = 0; i < vectorA.Length; i++)
        {
            dotProduct += vectorA[i] * vectorB[i];
            magnitudeA += vectorA[i] * vectorA[i];
            magnitudeB += vectorB[i] * vectorB[i];
        }

        magnitudeA = Math.Sqrt(magnitudeA);
        magnitudeB = Math.Sqrt(magnitudeB);

        if (magnitudeA == 0.0 || magnitudeB == 0.0)
            return 0.0;

        return dotProduct / (magnitudeA * magnitudeB);
    }
}
