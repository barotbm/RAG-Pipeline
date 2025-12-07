using RAG.Pipeline.Models;

namespace RAG.Pipeline.Interfaces;

/// <summary>
/// Abstraction for vector database storage and similarity search.
/// </summary>
public interface IVectorStore
{
    /// <summary>
    /// Indexes a collection of document chunks in the vector store.
    /// </summary>
    /// <param name="chunks">The chunks to index.</param>
    Task IndexChunksAsync(IEnumerable<DocumentChunk> chunks);

    /// <summary>
    /// Searches for chunks similar to the provided embedding vector.
    /// </summary>
    /// <param name="queryEmbedding">The query embedding vector.</param>
    /// <param name="topK">The number of top results to return.</param>
    /// <returns>A list of scored chunks ordered by similarity.</returns>
    Task<IReadOnlyList<ScoredChunk>> SearchByEmbeddingAsync(float[] queryEmbedding, int topK);
}
