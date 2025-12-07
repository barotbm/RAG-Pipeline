namespace RAG.Pipeline.Models;

/// <summary>
/// Represents a chunk of a document with multiple embeddings for different aspects.
/// </summary>
public class DocumentChunk
{
    /// <summary>
    /// Unique identifier for the chunk.
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// ID of the parent document.
    /// </summary>
    public string DocumentId { get; set; } = string.Empty;

    /// <summary>
    /// Index position of this chunk within the document.
    /// </summary>
    public int ChunkIndex { get; set; }

    /// <summary>
    /// The actual text content of this chunk.
    /// </summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// Title from the parent document.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Keywords associated with this chunk.
    /// </summary>
    public List<string> Keywords { get; set; } = new();

    /// <summary>
    /// LLM-generated summary of this chunk.
    /// </summary>
    public string Summary { get; set; } = string.Empty;

    /// <summary>
    /// Embedding vector for the chunk's text content.
    /// </summary>
    public float[] ContentEmbedding { get; set; } = Array.Empty<float>();

    /// <summary>
    /// Embedding vector for the chunk's title.
    /// </summary>
    public float[] TitleEmbedding { get; set; } = Array.Empty<float>();

    /// <summary>
    /// Embedding vector for the chunk's keywords.
    /// </summary>
    public float[] KeywordEmbedding { get; set; } = Array.Empty<float>();

    /// <summary>
    /// Embedding vector for the chunk's summary.
    /// </summary>
    public float[] SummaryEmbedding { get; set; } = Array.Empty<float>();

    /// <summary>
    /// Additional metadata from the parent document.
    /// </summary>
    public Dictionary<string, string> Metadata { get; set; } = new();
}
