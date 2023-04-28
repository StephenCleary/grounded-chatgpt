using Elastic.Clients.Elasticsearch;
using Elastic.Transport;
using PewBibleKjv.Text;
using server.Data.Elastic;

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
            if ((await _client.Indices.ExistsAsync("bible")).Exists)
                await _client.Indices.DeleteAsync("bible");

            await _client.Indices.CreateAsync("bible");

            foreach (var book in Structure.Books)
            {
                foreach (var chapter in book.Chapters)
                {
                    var documents = new List<object>();

                    for (int verse = chapter.BeginVerse; verse != chapter.EndVerse; ++verse)
                    {
                        var documentId = $"{book.Name} {chapter.Index + 1}:{verse - chapter.BeginVerse + 1}";
                        var text = Bible.FormattedVerse(verse).Text.Trim();
                        documents.Add(new SourceDocument
                        {
                            Id = documentId,
                            Text = text,
                            Url = "https://TODO",
                        });
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
