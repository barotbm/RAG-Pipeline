namespace RAG.Pipeline.Interfaces;

/// <summary>
/// Provides embedding generation services for text using a domain-tuned embedding model.
/// </summary>
public interface IEmbeddingProvider
{
    /// <summary>
    /// Generates an embedding vector for a single text input.
    /// </summary>
    /// <param name="text">The text to embed.</param>
    /// <returns>The embedding vector.</returns>
    Task<float[]> EmbedAsync(string text);

    /// <summary>
    /// Generates embedding vectors for multiple text inputs in a batch.
    /// </summary>
    /// <param name="texts">The collection of texts to embed.</param>
    /// <returns>An array of embedding vectors corresponding to each input text.</returns>
    Task<float[][]> EmbedBatchAsync(IEnumerable<string> texts);
}
