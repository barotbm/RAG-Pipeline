using RAG.Pipeline.Configuration;
using RAG.Pipeline.Interfaces;
using RAG.Pipeline.Models;
using Microsoft.Extensions.Options;

namespace RAG.Pipeline.Services;

/// <summary>
/// Handles document ingestion, chunking, embedding generation, and indexing.
/// </summary>
public class DocumentIngestionService
{
    private readonly IEmbeddingProvider _embeddingProvider;
    private readonly ILlmClient _llmClient;
    private readonly IVectorStore _vectorStore;
    private readonly IKeywordIndex _keywordIndex;
    private readonly IngestionOptions _options;
    private readonly ILogger<DocumentIngestionService> _logger;

    public DocumentIngestionService(
        IEmbeddingProvider embeddingProvider,
        ILlmClient llmClient,
        IVectorStore vectorStore,
        IKeywordIndex keywordIndex,
        IOptions<IngestionOptions> options,
        ILogger<DocumentIngestionService> logger)
    {
        _embeddingProvider = embeddingProvider;
        _llmClient = llmClient;
        _vectorStore = vectorStore;
        _keywordIndex = keywordIndex;
        _options = options.Value;
        _logger = logger;
    }

    /// <summary>
    /// Ingests a collection of documents by chunking, generating embeddings, and indexing.
    /// </summary>
    public async Task IngestDocumentsAsync(IEnumerable<Document> documents)
    {
        var allChunks = new List<DocumentChunk>();

        foreach (var document in documents)
        {
            _logger.LogInformation("Processing document: {Title} (ID: {Id})", document.Title, document.Id);

            try
            {
                var chunks = await ProcessDocumentAsync(document);
                allChunks.AddRange(chunks);
                
                _logger.LogInformation("Created {Count} chunks for document {Id}", chunks.Count, document.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process document {Id}", document.Id);
                throw;
            }
        }

        // Index all chunks
        _logger.LogInformation("Indexing {Count} total chunks", allChunks.Count);
        
        await _vectorStore.IndexChunksAsync(allChunks);
        await _keywordIndex.IndexChunksAsync(allChunks);

        _logger.LogInformation("Successfully ingested {DocCount} documents with {ChunkCount} chunks",
            documents.Count(), allChunks.Count);
    }

    /// <summary>
    /// Processes a single document into chunks with embeddings.
    /// </summary>
    private async Task<List<DocumentChunk>> ProcessDocumentAsync(Document document)
    {
        // Step 1: Split content into chunks
        var textChunks = ChunkText(document.Content);
        var chunks = new List<DocumentChunk>();

        for (int i = 0; i < textChunks.Count; i++)
        {
            var chunk = new DocumentChunk
            {
                Id = $"{document.Id}_chunk_{i}",
                DocumentId = document.Id,
                ChunkIndex = i,
                Text = textChunks[i],
                Title = document.Title,
                Keywords = new List<string>(document.Keywords),
                Metadata = new Dictionary<string, string>(document.Metadata)
            };

            // Step 2: Generate summary for the chunk
            if (_options.GenerateSummaries && !string.IsNullOrWhiteSpace(chunk.Text))
            {
                try
                {
                    chunk.Summary = await _llmClient.GenerateSummaryAsync(chunk.Text);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to generate summary for chunk {ChunkId}", chunk.Id);
                    chunk.Summary = chunk.Text.Length > 100 
                        ? chunk.Text.Substring(0, 100) + "..." 
                        : chunk.Text;
                }
            }

            // Step 3: Extract/generate keywords if not already present
            if (_options.ExtractKeywords && !chunk.Keywords.Any() && !string.IsNullOrWhiteSpace(chunk.Text))
            {
                try
                {
                    var extractedKeywords = await _llmClient.ExtractKeywordsAsync(chunk.Text);
                    chunk.Keywords.AddRange(extractedKeywords);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to extract keywords for chunk {ChunkId}", chunk.Id);
                }
            }

            chunks.Add(chunk);
        }

        // Step 4: Generate embeddings for all chunks in batch
        await GenerateEmbeddingsAsync(chunks);

        return chunks;
    }

    /// <summary>
    /// Generates multiple embeddings for each chunk (content, title, keywords, summary).
    /// </summary>
    private async Task GenerateEmbeddingsAsync(List<DocumentChunk> chunks)
    {
        // Prepare texts for batch embedding
        var contentTexts = chunks.Select(c => c.Text).ToList();
        var titleTexts = chunks.Select(c => c.Title).ToList();
        var keywordTexts = chunks.Select(c => string.Join(" ", c.Keywords)).ToList();
        var summaryTexts = chunks.Select(c => c.Summary).ToList();

        try
        {
            // Generate embeddings in batches
            var contentEmbeddings = await _embeddingProvider.EmbedBatchAsync(contentTexts);
            var titleEmbeddings = await _embeddingProvider.EmbedBatchAsync(titleTexts);
            var keywordEmbeddings = await _embeddingProvider.EmbedBatchAsync(keywordTexts);
            var summaryEmbeddings = await _embeddingProvider.EmbedBatchAsync(summaryTexts);

            // Assign embeddings to chunks
            for (int i = 0; i < chunks.Count; i++)
            {
                chunks[i].ContentEmbedding = contentEmbeddings[i];
                chunks[i].TitleEmbedding = titleEmbeddings[i];
                chunks[i].KeywordEmbedding = keywordEmbeddings[i];
                chunks[i].SummaryEmbedding = summaryEmbeddings[i];
            }

            _logger.LogInformation("Generated embeddings for {Count} chunks", chunks.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate embeddings");
            throw;
        }
    }

    /// <summary>
    /// Splits text into overlapping chunks.
    /// </summary>
    private List<string> ChunkText(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return new List<string>();

        var chunks = new List<string>();
        var chunkSize = _options.ChunkSize;
        var overlap = _options.ChunkOverlap;

        for (int i = 0; i < text.Length; i += chunkSize - overlap)
        {
            var remainingLength = text.Length - i;
            var currentChunkSize = Math.Min(chunkSize, remainingLength);
            
            var chunk = text.Substring(i, currentChunkSize);
            
            // Try to break at sentence or word boundary
            if (currentChunkSize == chunkSize && i + chunkSize < text.Length)
            {
                var lastPeriod = chunk.LastIndexOf('.');
                var lastSpace = chunk.LastIndexOf(' ');
                
                if (lastPeriod > chunkSize * 0.7)
                {
                    chunk = chunk.Substring(0, lastPeriod + 1);
                }
                else if (lastSpace > chunkSize * 0.7)
                {
                    chunk = chunk.Substring(0, lastSpace);
                }
            }

            chunks.Add(chunk.Trim());

            // Break if we've reached the end
            if (i + chunk.Length >= text.Length)
                break;
        }

        return chunks;
    }
}
