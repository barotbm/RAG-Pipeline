namespace RAG.Pipeline.Api.Dtos;

/// <summary>
/// Request DTO for document ingestion.
/// </summary>
public class IngestRequest
{
    public List<DocumentDto> Documents { get; set; } = new();
}

public class DocumentDto
{
    public string? Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public List<string>? Keywords { get; set; }
    public Dictionary<string, string>? Metadata { get; set; }
}
