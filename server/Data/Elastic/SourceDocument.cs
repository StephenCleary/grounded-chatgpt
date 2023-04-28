namespace server.Data.Elastic;

public sealed class SourceDocument
{
	public string Id { get; set; } = null!;
	public string Url { get; set; } = null!;
	public string Text { get; set; } = null!;
	public int TokenCount { get; set; }
}
