using RAG.Pipeline.Models;

namespace RAG.Pipeline.Interfaces;

/// <summary>
/// Orchestrates hybrid retrieval combining vector search, keyword search, and query expansion.
/// </summary>
public interface IRetrievalPipeline
{
    /// <summary>
    /// Retrieves the most relevant document chunks for a user query using hybrid search.
    /// </summary>
    /// <param name="userQuery">The user's search query.</param>
    /// <param name="topK">The number of top results to return.</param>
    /// <returns>A list of the most relevant document chunks.</returns>
    Task<IReadOnlyList<DocumentChunk>> RetrieveRelevantChunksAsync(string userQuery, int topK = 10);
}
