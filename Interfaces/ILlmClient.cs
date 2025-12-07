namespace RAG.Pipeline.Interfaces;

/// <summary>
/// Provides LLM-based services for query expansion and text summarization.
/// </summary>
public interface ILlmClient
{
    /// <summary>
    /// Generates multiple alternative phrasings of a user query for query expansion.
    /// </summary>
    /// <param name="userQuery">The original user query.</param>
    /// <returns>A list of expanded query variations.</returns>
    Task<IReadOnlyList<string>> GenerateQueryExpansionsAsync(string userQuery);

    /// <summary>
    /// Generates a concise summary of the provided text.
    /// </summary>
    /// <param name="text">The text to summarize.</param>
    /// <returns>A summary of the text.</returns>
    Task<string> GenerateSummaryAsync(string text);

    /// <summary>
    /// Extracts or generates relevant keywords from the provided text.
    /// </summary>
    /// <param name="text">The text to extract keywords from.</param>
    /// <returns>A list of keywords.</returns>
    Task<IReadOnlyList<string>> ExtractKeywordsAsync(string text);
}
