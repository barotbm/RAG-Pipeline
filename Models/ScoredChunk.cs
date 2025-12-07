namespace RAG.Pipeline.Models;

/// <summary>
/// Represents a document chunk with an associated relevance score.
/// </summary>
public class ScoredChunk
{
    /// <summary>
    /// The document chunk.
    /// </summary>
    public DocumentChunk Chunk { get; set; } = null!;

    /// <summary>
    /// Relevance score for this chunk (higher is more relevant).
    /// </summary>
    public double Score { get; set; }
}
