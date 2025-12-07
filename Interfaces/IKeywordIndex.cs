using RAG.Pipeline.Models;

namespace RAG.Pipeline.Interfaces;

/// <summary>
/// Abstraction for keyword-based search using BM25 or similar algorithms.
/// </summary>
public interface IKeywordIndex
{
    /// <summary>
    /// Indexes a collection of document chunks for keyword search.
    /// </summary>
    /// <param name="chunks">The chunks to index.</param>
    Task IndexChunksAsync(IEnumerable<DocumentChunk> chunks);

    /// <summary>
    /// Searches for chunks matching the keyword query using BM25 or similar scoring.
    /// </summary>
    /// <param name="query">The search query.</param>
    /// <param name="topK">The number of top results to return.</param>
    /// <returns>A list of scored chunks ordered by relevance.</returns>
    Task<IReadOnlyList<ScoredChunk>> SearchByKeywordAsync(string query, int topK);
}
