using PewBibleKjv.Text;

namespace server.Data;

public sealed class ResourceIndexer
{
    public ResourceIndexer(ElasticsearchService elasticsearchService, ILogger<ResourceIndexer> logger)
    {
        _elasticsearchService = elasticsearchService;
        _logger = logger;
    }

    public async Task IndexBibleAsync(IProgress<string> progress)
    {
        try
        {
            await _elasticsearchService.DeleteIndexAsync("bible");
            await _elasticsearchService.CreateIndexAsync("bible");

            foreach (var book in Structure.Books)
            {
                foreach (var chapter in book.Chapters)
                {
                    var documents = new List<SourceDocument>();

                    for (int verse = chapter.BeginVerse; verse != chapter.EndVerse; ++verse)
                    {
                        var documentId = $"{book.Name} {chapter.Index + 1}:{verse - chapter.BeginVerse + 1}";
                        var text = Bible.FormattedVerse(verse).Text.Trim();
                        documents.Add(new SourceDocument
                        {
                            Id = documentId,
                            Text = text,
                            Uri = "https://TODO",
                        });
                    }

                    await _elasticsearchService.IndexAsync("bible", documents);
                    progress?.Report($"Completed {book.Name} {chapter.Index + 1}");
                }
            }

            progress?.Report("Done!");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Indexing failed.");
            progress?.Report($"Failed: {ex}");
        }
    }

    private readonly ElasticsearchService _elasticsearchService;
    private readonly ILogger<ResourceIndexer> _logger;
}
