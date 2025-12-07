using RAG.Pipeline.Configuration;
using RAG.Pipeline.Interfaces;
using RAG.Pipeline.Models;
using Microsoft.Extensions.Options;

namespace RAG.Pipeline.Services;

/// <summary>
/// Orchestrates hybrid retrieval combining vector search, keyword search, and query expansion.
/// </summary>
public class RetrievalPipeline : IRetrievalPipeline
{
    private readonly IEmbeddingProvider _embeddingProvider;
    private readonly ILlmClient _llmClient;
    private readonly IVectorStore _vectorStore;
    private readonly IKeywordIndex _keywordIndex;
    private readonly RetrievalOptions _options;
    private readonly ILogger<RetrievalPipeline> _logger;

    public RetrievalPipeline(
        IEmbeddingProvider embeddingProvider,
        ILlmClient llmClient,
        IVectorStore vectorStore,
        IKeywordIndex keywordIndex,
        IOptions<RetrievalOptions> options,
        ILogger<RetrievalPipeline> logger)
    {
        _embeddingProvider = embeddingProvider;
        _llmClient = llmClient;
        _vectorStore = vectorStore;
        _keywordIndex = keywordIndex;
        _options = options.Value;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<DocumentChunk>> RetrieveRelevantChunksAsync(string userQuery, int topK = 10)
    {
        _logger.LogInformation("Starting retrieval for query: {Query}", userQuery);

        // Step 1: Generate query expansions
        var queries = new List<string> { userQuery };
        
        if (_options.EnableQueryExpansion)
        {
            try
            {
                var expansions = await _llmClient.GenerateQueryExpansionsAsync(userQuery);
                queries.AddRange(expansions.Take(_options.QueryExpansionCount));
                _logger.LogInformation("Generated {Count} query expansions", expansions.Count);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to generate query expansions, continuing with original query");
            }
        }

        // Step 2: Perform vector search for all queries
        var vectorResults = new Dictionary<string, ScoredChunk>();
        
        foreach (var query in queries)
        {
            try
            {
                var embedding = await _embeddingProvider.EmbedAsync(query);
                var results = await _vectorStore.SearchByEmbeddingAsync(embedding, _options.SearchTopK);
                
                foreach (var result in results)
                {
                    var chunkId = result.Chunk.Id;
                    if (!vectorResults.ContainsKey(chunkId) || vectorResults[chunkId].Score < result.Score)
                    {
                        vectorResults[chunkId] = result;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Vector search failed for query: {Query}", query);
            }
        }

        _logger.LogInformation("Vector search found {Count} unique chunks", vectorResults.Count);

        // Step 3: Perform keyword search (BM25) on original query
        var keywordResults = new Dictionary<string, ScoredChunk>();
        
        try
        {
            var results = await _keywordIndex.SearchByKeywordAsync(userQuery, _options.SearchTopK);
            foreach (var result in results)
            {
                keywordResults[result.Chunk.Id] = result;
            }
            
            _logger.LogInformation("Keyword search found {Count} chunks", keywordResults.Count);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Keyword search failed");
        }

        // Step 4: Hybrid fusion - combine and normalize scores
        var hybridScores = FuseScores(vectorResults, keywordResults);

        // Step 5: Return top K results
        var finalResults = hybridScores
            .OrderByDescending(kvp => kvp.Value.Score)
            .Take(topK)
            .Select(kvp => kvp.Value.Chunk)
            .ToList();

        _logger.LogInformation("Returning {Count} final results", finalResults.Count);
        return finalResults;
    }

    /// <summary>
    /// Fuses vector and keyword search scores using weighted combination.
    /// </summary>
    private Dictionary<string, ScoredChunk> FuseScores(
        Dictionary<string, ScoredChunk> vectorResults,
        Dictionary<string, ScoredChunk> keywordResults)
    {
        var fused = new Dictionary<string, ScoredChunk>();

        // Normalize scores to [0, 1] range
        var maxVectorScore = vectorResults.Any() ? vectorResults.Values.Max(sc => sc.Score) : 1.0;
        var maxKeywordScore = keywordResults.Any() ? keywordResults.Values.Max(sc => sc.Score) : 1.0;

        // Combine all unique chunk IDs
        var allChunkIds = vectorResults.Keys.Union(keywordResults.Keys).ToHashSet();

        foreach (var chunkId in allChunkIds)
        {
            var vectorScore = vectorResults.TryGetValue(chunkId, out var vs) 
                ? vs.Score / maxVectorScore 
                : 0.0;
            
            var keywordScore = keywordResults.TryGetValue(chunkId, out var ks) 
                ? ks.Score / maxKeywordScore 
                : 0.0;

            // Weighted hybrid score
            var hybridScore = (_options.VectorSearchWeight * vectorScore) + 
                            (_options.KeywordSearchWeight * keywordScore);

            var chunk = vectorResults.TryGetValue(chunkId, out var vChunk) 
                ? vChunk.Chunk 
                : keywordResults[chunkId].Chunk;

            fused[chunkId] = new ScoredChunk
            {
                Chunk = chunk,
                Score = hybridScore
            };
        }

        return fused;
    }
}
