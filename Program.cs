using RAG.Pipeline.Configuration;
using RAG.Pipeline.Interfaces;
using RAG.Pipeline.Services;
using RAG.Pipeline.Services.Mock;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "RAG Pipeline API", Version = "v1" });
});

// Configure options
builder.Services.Configure<RetrievalOptions>(options =>
{
    options.VectorSearchWeight = 0.7;
    options.KeywordSearchWeight = 0.3;
    options.QueryExpansionCount = 5;
    options.EnableQueryExpansion = true;
    options.SearchTopK = 20;
});

builder.Services.Configure<IngestionOptions>(options =>
{
    options.ChunkSize = 500;
    options.ChunkOverlap = 50;
    options.GenerateSummaries = true;
    options.ExtractKeywords = true;
});

// Register core services
// Note: Replace Mock implementations with real ones (OpenAI, Azure OpenAI, etc.)
builder.Services.AddSingleton<IEmbeddingProvider, MockEmbeddingProvider>();
builder.Services.AddSingleton<ILlmClient, MockLlmClient>();
builder.Services.AddSingleton<IVectorStore, InMemoryVectorStore>();
builder.Services.AddSingleton<IKeywordIndex, InMemoryKeywordIndex>();
builder.Services.AddScoped<IRetrievalPipeline, RetrievalPipeline>();
builder.Services.AddScoped<DocumentIngestionService>();

// Add logging
builder.Services.AddLogging(logging =>
{
    logging.AddConsole();
    logging.AddDebug();
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.MapControllers();

app.Logger.LogInformation("RAG Pipeline API started successfully");

app.Run();
