namespace server.Data;

/// <summary>
/// A document stored in the search index that is used as source information for grounding the chat AI.
/// </summary>
public sealed class SourceDocument
{
    /// <summary>
    /// The unique id of the document within the search index.
    /// </summary>
    public string Id { get; set; } = null!;

    /// <summary>
    /// The URI used to generate links from source references.
    /// </summary>
    public string Uri { get; set; } = null!;

    /// <summary>
    /// The text of the source material. This is used to match the user's query and also provided to the chat AI as a grounding source.
    /// </summary>
    public string Text { get; set; } = null!;
}
