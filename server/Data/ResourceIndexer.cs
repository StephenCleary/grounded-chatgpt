using HtmlAgilityPack;
using PewBibleKjv.Text;
using System.Text.RegularExpressions;
using UglyToad.PdfPig;
using Structure = PewBibleKjv.Text.Structure;

namespace server.Data;

public sealed class ResourceIndexer
{
    public ResourceIndexer(ElasticsearchService elasticsearchService, ILogger<ResourceIndexer> logger)
    {
        _elasticsearchService = elasticsearchService;
		_logger = logger;
    }

    public async Task IndexBibleAsync(string indexName, IProgress<string> progress)
    {
        foreach (var book in Structure.Books)
        {
            foreach (var chapter in book.Chapters)
            {
                var documents = new List<SourceDocument>();

                for (int verse = chapter.BeginVerse; verse != chapter.EndVerse; ++verse)
                {
                    var documentId = $"{book.Name} {chapter.Index + 1}:{verse - chapter.BeginVerse + 1}";
                    var text = PreprocessText(Bible.FormattedVerse(verse).Text);
                    documents.Add(new SourceDocument
                    {
                        Id = documentId,
                        Text = text,
                        Uri = $"https://ref.ly/r/kjv/{Uri.EscapeDataString(documentId)}",
                        Name = documentId,
                    });
                }

                await _elasticsearchService.IndexAsync(indexName, documents);
                progress?.Report($"Completed {book.Name} {chapter.Index + 1}");
            }
        }
    }

    public async Task IndexUriAsync(string indexName, string uri, IProgress<string> progress)
    {
        progress?.Report($"Downloading {uri}");
        var html = await Globals.HttpClient.GetStringAsync(uri);

        progress?.Report($"Processing {uri}");
        var doc = new HtmlDocument();
        doc.LoadHtml(html);
        var text = string.Join(" ", doc.DocumentNode.SelectSingleNode("//body")
            .Descendants()
            .Where(n => !n.HasChildNodes && !string.IsNullOrWhiteSpace(n.InnerText))
            .Select(n => HtmlEntity.DeEntitize(n.InnerText)));
        text = PreprocessText(text);

        progress?.Report($"Indexing {uri}");
        var documentId = doc.DocumentNode.SelectSingleNode("//title")?.InnerText ?? uri;
        await IndexChunks(indexName, new SourceDocument
        {
            Id = documentId,
            Text = text,
            Uri = uri,
            Name = documentId,
        });
    }

    public async Task IndexPdfAsync(string indexName, string name, string path, string uri, IProgress<string> progress)
    {
        progress?.Report($"Extracting text from {name}");
        using var doc = PdfDocument.Open(path);
        var text = string.Join(" ", doc.GetPages().SelectMany(x => x.GetWords()));
        text = PreprocessText(text);

        progress?.Report($"Indexing {name}");
        await IndexChunks(indexName, new SourceDocument
        {
            Id = name,
            Text = text,
            Uri = uri,
            Name = name,
        });
    }

    /// <summary>
    /// Replace all '\r', '\n', '\t' with ' ', and replace consecutive runs of spaces with single spaces.
    /// </summary>
    private static string PreprocessText(string text)
    {
        text = text.Replace('\r', ' ').Replace('\n', ' ').Replace('\t', ' ');
        return Regex.Replace(text, " +", " ").Trim();
    }

    private async Task IndexChunks(string indexName, SourceDocument sourceDocument)
    {
        // Split the source document into chunks, then batch-index them into Elasticsearch.
        foreach (var batch in Split(sourceDocument).Chunk(20))
            await _elasticsearchService.IndexAsync(indexName, batch);
    }

    private IEnumerable<SourceDocument> Split(SourceDocument sourceDocument)
    {
        var tokens = Model.Gpt35Turbo.TokenCount(sourceDocument.Text);
        if (tokens < MaxSourceTokenLength)
        {
            yield return sourceDocument;
            yield break;
        }

        // Split it approximately in half, but on a sentence boundary if possible; if not, on a word boundary; if not, just in half.
        var splitIndex = TryFindSplitIndex(SentenceEndings, sourceDocument.Text);
        if (splitIndex == -1)
            splitIndex = TryFindSplitIndex(WordBreaks, sourceDocument.Text);
        if (splitIndex <= 0 || splitIndex >= sourceDocument.Text.Length)
            splitIndex = sourceDocument.Text.Length / 2;

        var first = new SourceDocument
        {
            Id = sourceDocument.Id + "0",
            Text = sourceDocument.Text[..splitIndex].Trim(),
            Uri = sourceDocument.Uri,
            Name = sourceDocument.Name,
        };
        var second = new SourceDocument
		{
			Id = sourceDocument.Id + "1",
			Text = sourceDocument.Text[splitIndex..].Trim(),
			Uri = sourceDocument.Uri,
			Name = sourceDocument.Name,
		};
		
        foreach (var firstResult in Split(first))
            yield return firstResult;
        foreach (var secondResult in Split(second))
            yield return secondResult;

        static int TryFindSplitIndex(char[] delimiters, string text)
        {
			var halfIndex = text.Length / 2;
			var previousSentenceEnd = text.LastIndexOfAny(delimiters, halfIndex);
			var nextSentenceEnd = text.IndexOfAny(delimiters, halfIndex);
            if (previousSentenceEnd == -1 && nextSentenceEnd == -1)
                return -1;
            if (previousSentenceEnd == -1)
                return nextSentenceEnd + 1;
            if (nextSentenceEnd == -1)
                return previousSentenceEnd + 1;
            return halfIndex - previousSentenceEnd < nextSentenceEnd - halfIndex ? previousSentenceEnd + 1 : nextSentenceEnd + 1;
		}
	}

    private readonly ElasticsearchService _elasticsearchService;
	private readonly ILogger<ResourceIndexer> _logger;

    private const int MaxSourceTokenLength = 256;
    private static readonly char[] SentenceEndings = new[] { '.', '!', '?' };
    private static readonly char[] WordBreaks = new[] { ',', ';', ':', ' ', '(', ')', '[', ']', '{', '}', '\t', '\n' };
}
