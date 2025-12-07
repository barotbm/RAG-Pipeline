namespace RAG.Pipeline.Configuration;

/// <summary>
/// Configuration options for document ingestion and chunking.
/// </summary>
public class IngestionOptions
{
    /// <summary>
    /// Maximum number of characters per chunk.
    /// Default is 500.
    /// </summary>
    public int ChunkSize { get; set; } = 500;

    /// <summary>
    /// Number of overlapping characters between chunks.
    /// Default is 50.
    /// </summary>
    public int ChunkOverlap { get; set; } = 50;

    /// <summary>
    /// Whether to generate summaries for each chunk using LLM.
    /// Default is true.
    /// </summary>
    public bool GenerateSummaries { get; set; } = true;

    /// <summary>
    /// Whether to extract/generate keywords for each chunk.
    /// Default is true.
    /// </summary>
    public bool ExtractKeywords { get; set; } = true;
}
