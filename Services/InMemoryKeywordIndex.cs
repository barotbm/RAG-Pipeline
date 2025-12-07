using RAG.Pipeline.Interfaces;
using RAG.Pipeline.Models;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;

namespace RAG.Pipeline.Services;

/// <summary>
/// In-memory implementation of keyword search using BM25 scoring algorithm.
/// </summary>
public class InMemoryKeywordIndex : IKeywordIndex
{
    private readonly ConcurrentDictionary<string, DocumentChunk> _chunks = new();
    private readonly ConcurrentDictionary<string, Dictionary<string, int>> _invertedIndex = new();
    private readonly ConcurrentDictionary<string, int> _documentLengths = new();
    private double _avgDocumentLength = 0;
    private readonly ILogger<InMemoryKeywordIndex> _logger;

    // BM25 parameters
    private const double K1 = 1.5;
    private const double B = 0.75;

    public InMemoryKeywordIndex(ILogger<InMemoryKeywordIndex> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc/>
    public Task IndexChunksAsync(IEnumerable<DocumentChunk> chunks)
    {
        foreach (var chunk in chunks)
        {
            _chunks[chunk.Id] = chunk;

            // Combine text, title, keywords, and summary for indexing
            var indexableText = $"{chunk.Text} {chunk.Title} {string.Join(" ", chunk.Keywords)} {chunk.Summary}";
            var terms = Tokenize(indexableText);
            
            _documentLengths[chunk.Id] = terms.Count;

            foreach (var term in terms.Distinct())
            {
                if (!_invertedIndex.ContainsKey(term))
                {
                    _invertedIndex[term] = new Dictionary<string, int>();
                }

                // Count term frequency in this document
                var termFreq = terms.Count(t => t == term);
                _invertedIndex[term][chunk.Id] = termFreq;
            }
        }

        // Calculate average document length
        if (_documentLengths.Any())
        {
            _avgDocumentLength = _documentLengths.Values.Average();
        }

        _logger.LogInformation("Indexed {Count} chunks in keyword index", _chunks.Count);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task<IReadOnlyList<ScoredChunk>> SearchByKeywordAsync(string query, int topK)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return Task.FromResult<IReadOnlyList<ScoredChunk>>(Array.Empty<ScoredChunk>());
        }

        var queryTerms = Tokenize(query);
        var scoredChunks = new Dictionary<string, double>();

        foreach (var term in queryTerms.Distinct())
        {
            if (!_invertedIndex.TryGetValue(term, out var postingsList))
                continue;

            // Calculate IDF (Inverse Document Frequency)
            var documentFrequency = postingsList.Count;
            var idf = Math.Log((_chunks.Count - documentFrequency + 0.5) / (documentFrequency + 0.5) + 1.0);

            foreach (var (chunkId, termFreq) in postingsList)
            {
                if (!_documentLengths.TryGetValue(chunkId, out var docLength))
                    continue;

                // Calculate BM25 score component for this term
                var normalizedLength = docLength / _avgDocumentLength;
                var bm25Component = idf * (termFreq * (K1 + 1)) / 
                                   (termFreq + K1 * (1 - B + B * normalizedLength));

                if (!scoredChunks.ContainsKey(chunkId))
                {
                    scoredChunks[chunkId] = 0;
                }

                scoredChunks[chunkId] += bm25Component;
            }
        }

        var results = scoredChunks
            .Select(kvp => new ScoredChunk 
            { 
                Chunk = _chunks[kvp.Key], 
                Score = kvp.Value 
            })
            .OrderByDescending(sc => sc.Score)
            .Take(topK)
            .ToList();

        _logger.LogInformation("Keyword search returned {Count} results", results.Count);
        return Task.FromResult<IReadOnlyList<ScoredChunk>>(results);
    }

    /// <summary>
    /// Tokenizes text into lowercase terms.
    /// </summary>
    private static List<string> Tokenize(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return new List<string>();

        // Simple tokenization: lowercase, split on non-alphanumeric, remove stopwords
        var tokens = Regex.Split(text.ToLowerInvariant(), @"\W+")
            .Where(t => t.Length > 2 && !IsStopWord(t))
            .ToList();

        return tokens;
    }

    /// <summary>
    /// Checks if a word is a common stopword.
    /// </summary>
    private static bool IsStopWord(string word)
    {
        var stopWords = new HashSet<string>
        {
            "the", "is", "at", "which", "on", "a", "an", "and", "or", "but", 
            "in", "with", "to", "for", "of", "as", "by", "that", "this", "it",
            "from", "are", "was", "were", "been", "be", "have", "has", "had",
            "do", "does", "did", "will", "would", "could", "should", "may", "might"
        };

        return stopWords.Contains(word);
    }
}
