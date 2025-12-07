using Microsoft.AspNetCore.Mvc;
using RAG.Pipeline.Api.Dtos;
using RAG.Pipeline.Interfaces;
using RAG.Pipeline.Models;
using RAG.Pipeline.Services;

namespace RAG.Pipeline.Api.Controllers;

/// <summary>
/// API endpoints for the RAG retrieval pipeline.
/// </summary>
[ApiController]
[Route("api")]
public class RagController : ControllerBase
{
    private readonly DocumentIngestionService _ingestionService;
    private readonly IRetrievalPipeline _retrievalPipeline;
    private readonly ILlmClient _llmClient;
    private readonly ILogger<RagController> _logger;

    public RagController(
        DocumentIngestionService ingestionService,
        IRetrievalPipeline retrievalPipeline,
        ILlmClient llmClient,
        ILogger<RagController> logger)
    {
        _ingestionService = ingestionService;
        _retrievalPipeline = retrievalPipeline;
        _llmClient = llmClient;
        _logger = logger;
    }

    /// <summary>
    /// Ingests documents into the RAG pipeline.
    /// </summary>
    [HttpPost("ingest")]
    public async Task<IActionResult> IngestDocuments([FromBody] IngestRequest request)
    {
        if (request?.Documents == null || !request.Documents.Any())
        {
            return BadRequest(new { error = "No documents provided" });
        }

        try
        {
            _logger.LogInformation("Ingesting {Count} documents", request.Documents.Count);

            // Map DTOs to domain models
            var documents = request.Documents.Select(dto => new Document
            {
                Id = dto.Id ?? Guid.NewGuid().ToString(),
                Title = dto.Title,
                Content = dto.Content,
                Keywords = dto.Keywords ?? new List<string>(),
                Metadata = dto.Metadata ?? new Dictionary<string, string>()
            }).ToList();

            await _ingestionService.IngestDocumentsAsync(documents);

            return Ok(new
            {
                success = true,
                documentsIngested = documents.Count,
                message = "Documents successfully ingested and indexed"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to ingest documents");
            return StatusCode(500, new { error = "Failed to ingest documents", details = ex.Message });
        }
    }

    /// <summary>
    /// Retrieves relevant chunks for a given query using hybrid search.
    /// </summary>
    [HttpPost("ask")]
    public async Task<IActionResult> Ask([FromBody] AskRequest request)
    {
        if (string.IsNullOrWhiteSpace(request?.Query))
        {
            return BadRequest(new { error = "Query is required" });
        }

        try
        {
            _logger.LogInformation("Processing query: {Query}", request.Query);

            // Generate query expansions for response
            var expandedQueries = new List<string>();
            try
            {
                var expansions = await _llmClient.GenerateQueryExpansionsAsync(request.Query);
                expandedQueries.AddRange(expansions);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to generate query expansions for response");
            }

            // Retrieve relevant chunks
            var chunks = await _retrievalPipeline.RetrieveRelevantChunksAsync(request.Query, request.TopK);

            // Map to DTOs
            var response = new AskResponse
            {
                OriginalQuery = request.Query,
                ExpandedQueries = expandedQueries,
                RetrievedChunks = chunks.Select(chunk => new RetrievedChunkDto
                {
                    ChunkId = chunk.Id,
                    DocumentId = chunk.DocumentId,
                    Title = chunk.Title,
                    Text = chunk.Text,
                    ChunkIndex = chunk.ChunkIndex,
                    Keywords = chunk.Keywords,
                    Summary = chunk.Summary,
                    Metadata = chunk.Metadata
                }).ToList()
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process query");
            return StatusCode(500, new { error = "Failed to process query", details = ex.Message });
        }
    }

    /// <summary>
    /// Health check endpoint.
    /// </summary>
    [HttpGet("health")]
    public IActionResult Health()
    {
        return Ok(new { status = "healthy", timestamp = DateTime.UtcNow });
    }
}
