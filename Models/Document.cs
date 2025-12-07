namespace RAG.Pipeline.Models;

/// <summary>
/// Represents a source document to be ingested and chunked for retrieval.
/// </summary>
public class Document
{
    /// <summary>
    /// Unique identifier for the document.
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Title of the document.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Full text content of the document.
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Keywords associated with the document for enhanced retrieval.
    /// </summary>
    public List<string> Keywords { get; set; } = new();

    /// <summary>
    /// Additional metadata key-value pairs for the document.
    /// </summary>
    public Dictionary<string, string> Metadata { get; set; } = new();
}
