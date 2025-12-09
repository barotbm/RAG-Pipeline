# RAG Pipeline - Production-Grade Retrieval-Augmented Generation in C# (.NET 8)

## Overview

This is a production-ready RAG (Retrieval-Augmented Generation) pipeline implementation in C# with .NET 8, featuring:

- **Multi-embedding strategy**: Each document chunk has embeddings for content, title, keywords, and summary
- **Hybrid search**: Combines BM25 keyword search with vector similarity search using configurable score fusion
- **Query expansion**: Uses LLM to generate alternative phrasings of user queries for improved recall
- **Clean architecture**: Interface-driven design with dependency injection for easy testing and customization

## Architecture

### Core Components

1. **Models** (`Models/`)
   - `Document`: Source document with title, content, keywords, and metadata
   - `DocumentChunk`: Chunked document with multiple embeddings
   - `ScoredChunk`: Search result with relevance score

2. **Interfaces** (`Interfaces/`)
   - `IEmbeddingProvider`: Domain-tuned embedding generation (BGE, E5, OpenAI, etc.)
   - `ILlmClient`: LLM services for query expansion, summarization, and keyword extraction
   - `IVectorStore`: Vector database abstraction (Pinecone, Qdrant, pgvector, Azure AI Search)
   - `IKeywordIndex`: BM25/inverted index abstraction (Lucene, Elasticsearch, Azure Search)
   - `IRetrievalPipeline`: Orchestrates hybrid retrieval

3. **Services** (`Services/`)
   - `DocumentIngestionService`: Chunks documents, generates embeddings, and indexes
   - `RetrievalPipeline`: Hybrid search with query expansion and score fusion
   - `InMemoryVectorStore`: In-memory vector search with cosine similarity
   - `InMemoryKeywordIndex`: In-memory BM25 implementation
   - Mock implementations for testing (replace with production providers)

4. **API** (`Api/`)
   - `POST /api/ingest`: Ingest documents
   - `POST /api/ask`: Query for relevant chunks
   - `GET /api/health`: Health check

## Configuration

### Retrieval Options

```csharp
builder.Services.Configure<RetrievalOptions>(options =>
{
    options.VectorSearchWeight = 0.7;      // Weight for semantic similarity
    options.KeywordSearchWeight = 0.3;     // Weight for keyword match
    options.QueryExpansionCount = 5;       // Number of query variations
    options.EnableQueryExpansion = true;   // Enable/disable expansion
    options.SearchTopK = 20;               // Results per search method
});
```

### Ingestion Options

```csharp
builder.Services.Configure<IngestionOptions>(options =>
{
    options.ChunkSize = 500;               // Characters per chunk
    options.ChunkOverlap = 50;             // Overlapping characters
    options.GenerateSummaries = true;      // Generate LLM summaries
    options.ExtractKeywords = true;        // Extract keywords
});
```

## Getting Started

### 1. Run the API

```bash
dotnet run --project RAG.Pipeline.csproj
```

The API will start at `https://localhost:5001` (or `http://localhost:5000`).

### 2. Ingest Documents

```bash
curl -X POST https://localhost:5001/api/ingest \
  -H "Content-Type: application/json" \
  -d '{
    "documents": [
      {
        "title": "Escrow Account Information",
        "content": "An escrow account is used to pay property taxes and insurance. The mortgage servicer collects monthly payments and disburses them when due. An escrow shortage occurs when the account balance is insufficient to cover upcoming payments.",
        "keywords": ["escrow", "property taxes", "insurance", "mortgage"]
      }
    ]
  }'
```

### 3. Query for Relevant Chunks

```bash
curl -X POST https://localhost:5001/api/ask \
  -H "Content-Type: application/json" \
  -d '{
    "query": "How is escrow shortage calculated?",
    "topK": 5
  }'
```

Response:
```json
{
  "originalQuery": "How is escrow shortage calculated?",
  "expandedQueries": [
    "What is How is escrow shortage calculated??",
    "Explain How is escrow shortage calculated?",
    "How does How is escrow shortage calculated? work?",
    "Information about How is escrow shortage calculated?",
    "Details on How is escrow shortage calculated?"
  ],
  "retrievedChunks": [
    {
      "chunkId": "doc1_chunk_0",
      "documentId": "doc1",
      "title": "Escrow Account Information",
      "text": "An escrow account is used to pay property taxes...",
      "chunkIndex": 0,
      "keywords": ["escrow", "property", "taxes"],
      "summary": "An escrow account is used to pay property taxes and insurance...",
      "metadata": {}
    }
  ]
}
```

## Production Deployment

### Replace Mock Implementations

The project includes mock implementations for testing. Replace these with production services:

#### 1. Embedding Provider

Replace `MockEmbeddingProvider` with:
- **OpenAI**: Use `text-embedding-ada-002` or `text-embedding-3-small`
- **Azure OpenAI**: Same as OpenAI but hosted on Azure
- **BGE/E5**: Use local sentence transformers or API

```csharp
// Example: OpenAI
builder.Services.AddSingleton<IEmbeddingProvider, OpenAIEmbeddingProvider>();
```

#### 2. LLM Client

Replace `MockLlmClient` with:
- **OpenAI**: GPT-4, GPT-3.5-turbo
- **Azure OpenAI**: Same models on Azure
- **Anthropic**: Claude models
- **Local**: Ollama, llama.cpp

```csharp
// Example: OpenAI
builder.Services.AddSingleton<ILlmClient, OpenAILlmClient>();
```

#### 3. Vector Store

Replace `InMemoryVectorStore` with:
- **Pinecone**: Managed vector database
- **Qdrant**: Open-source vector search
- **pgvector**: PostgreSQL extension
- **Azure AI Search**: Azure's vector search
- **Milvus**: Scalable vector database

```csharp
// Example: Pinecone
builder.Services.AddSingleton<IVectorStore, PineconeVectorStore>();
```

#### 4. Keyword Index

Replace `InMemoryKeywordIndex` with:
- **Elasticsearch**: Full-text search engine
- **Azure Cognitive Search**: Azure's search service
- **Lucene.NET**: .NET port of Apache Lucene
- **SQL Server Full-Text Search**: Built into SQL Server

```csharp
// Example: Elasticsearch
builder.Services.AddSingleton<IKeywordIndex, ElasticsearchKeywordIndex>();
```

## Key Features

### 1. Multi-Embedding Strategy

Each chunk has four embeddings:
- **Content**: The chunk's text
- **Title**: Document title
- **Keywords**: Extracted/provided keywords
- **Summary**: LLM-generated summary

This improves retrieval by matching queries against different aspects of the content.

### 2. Hybrid Search

Combines two complementary approaches:
- **Vector Search**: Semantic similarity using embeddings
- **Keyword Search**: Exact/fuzzy term matching using BM25

Scores are normalized and fused using configurable weights:
```
hybridScore = α × vectorScore + β × keywordScore
```

### 3. Query Expansion

The LLM generates alternative phrasings of the user's query:
- "How is escrow calculated?" → "Explain escrow calculation", "What determines escrow amount?", etc.

This improves recall by capturing different ways users might express their intent.

### 4. Clean Architecture

- **Interfaces**: All external dependencies are abstracted
- **Dependency Injection**: Easy to swap implementations
- **Testability**: Mock implementations provided
- **Separation of Concerns**: Clear responsibility boundaries

## Testing

### Unit Tests Example

```csharp
[Fact]
public async Task RetrievalPipeline_WithQueryExpansion_ReturnsRelevantChunks()
{
    // Arrange
    var mockEmbedding = new MockEmbeddingProvider();
    var mockLlm = new MockLlmClient();
    var vectorStore = new InMemoryVectorStore();
    var keywordIndex = new InMemoryKeywordIndex();
    
    var pipeline = new RetrievalPipeline(
        mockEmbedding, mockLlm, vectorStore, 
        keywordIndex, options, logger);
    
    // Act
    var results = await pipeline.RetrieveRelevantChunksAsync("test query");
    
    // Assert
    Assert.NotEmpty(results);
}
```

## Performance Tuning

1. **Chunk Size**: Smaller chunks (200-500 chars) for precise retrieval, larger (500-1000) for context
2. **Vector Weight**: Higher (0.7-0.8) for semantic understanding, lower for exact term matching
3. **Keyword Weight**: Higher (0.4-0.5) for technical/exact terms, lower for conceptual queries
4. **Query Expansion**: 3-7 expansions for balance between recall and latency
5. **Search TopK**: Retrieve 2-3x more results than needed for better fusion results

## Monitoring

Key metrics to track:
- Query latency (p50, p95, p99)
- Embedding generation time
- Index size and query performance
- Retrieval precision@k
- Cache hit rates (if caching embeddings)

## License

MIT

## Contributing

Contributions welcome! Please follow clean architecture principles and include tests.
