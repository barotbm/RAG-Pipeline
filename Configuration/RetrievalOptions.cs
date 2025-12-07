namespace RAG.Pipeline.Configuration;

/// <summary>
/// Configuration options for the retrieval pipeline.
/// </summary>
public class RetrievalOptions
{
    /// <summary>
    /// Weight for vector search scores in hybrid fusion (0.0 to 1.0).
    /// Default is 0.7, giving more weight to semantic similarity.
    /// </summary>
    public double VectorSearchWeight { get; set; } = 0.7;

    /// <summary>
    /// Weight for keyword search scores in hybrid fusion (0.0 to 1.0).
    /// Default is 0.3.
    /// </summary>
    public double KeywordSearchWeight { get; set; } = 0.3;

    /// <summary>
    /// Number of query expansions to generate for each user query.
    /// Default is 5.
    /// </summary>
    public int QueryExpansionCount { get; set; } = 5;

    /// <summary>
    /// Whether to enable query expansion.
    /// Default is true.
    /// </summary>
    public bool EnableQueryExpansion { get; set; } = true;

    /// <summary>
    /// Number of results to retrieve from each search method before fusion.
    /// Default is 20.
    /// </summary>
    public int SearchTopK { get; set; } = 20;
}
