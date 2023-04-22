using Elastic.Clients.Elasticsearch;
using Elastic.Transport;
using PewBibleKjv.Text;

namespace server.Data;

public sealed class ResourceIndexer
{
    public ResourceIndexer(ElasticsearchClient client, ILogger<ResourceIndexer> logger)
    {
        _client = client;
        _logger = logger;
    }

    public async Task IndexBibleAsync(IProgress<string> progress)
    {
        try
        {
            await _client.Indices.CreateAsync("bible");

            foreach (var book in Structure.Books)
            {
                foreach (var chapter in book.Chapters)
                {
                    var documents = new List<object>();

                    for (int verse = chapter.BeginVerse; verse != chapter.EndVerse; ++verse)
                    {
                        var documentId = $"{book.Name} {chapter.Index + 1}:{verse - chapter.BeginVerse + 1}";
                        documents.Add(new { id = documentId, text = Bible.FormattedVerse(verse).Text.Trim() });
                    }

                    await _client.IndexManyAsync(documents, "bible");
                    progress?.Report($"Completed {book.Name} {chapter.Index + 1}");
                }
            }
        }
        catch (TransportException ex) when (ex.ApiCallDetails.HttpStatusCode == 400)
        {
            progress?.Report("Already indexed.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Indexing failed.");
            progress?.Report($"Failed: {ex}");
        }
    }

    private readonly ElasticsearchClient _client;
    private readonly ILogger<ResourceIndexer> _logger;
}
