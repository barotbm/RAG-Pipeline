namespace RAG.Pipeline.Api.Dtos;

/// <summary>
/// Request DTO for retrieval query.
/// </summary>
public class AskRequest
{
    public string Query { get; set; } = string.Empty;
    public int TopK { get; set; } = 10;
}
