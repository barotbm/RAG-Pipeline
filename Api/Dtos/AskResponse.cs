namespace RAG.Pipeline.Api.Dtos;

/// <summary>
/// Response DTO for retrieval query.
/// </summary>
public class AskResponse
{
    public string OriginalQuery { get; set; } = string.Empty;
    public List<string> ExpandedQueries { get; set; } = new();
    public List<RetrievedChunkDto> RetrievedChunks { get; set; } = new();
}

public class RetrievedChunkDto
{
    public string ChunkId { get; set; } = string.Empty;
    public string DocumentId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public int ChunkIndex { get; set; }
    public List<string> Keywords { get; set; } = new();
    public string Summary { get; set; } = string.Empty;
    public Dictionary<string, string> Metadata { get; set; } = new();
}
