using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.IndexManagement;
using Elastic.Transport;
using System.Text;

namespace server.Data;

public sealed class ElasticsearchService
{
	public ElasticsearchService(ILogger<ElasticsearchService> logger)
	{
		// Elasticsearch: log request and response bodies, and throw exceptions on errors.
		_client = new ElasticsearchClient(new ElasticsearchClientSettings()
			.ThrowExceptions()
			.DisableDirectStreaming()
			.OnRequestCompleted(details =>
			{
				var request = details.RequestBodyInBytes == null ? null : Encoding.UTF8.GetString(details.RequestBodyInBytes);
				var response = details.ResponseBodyInBytes == null ? null : Encoding.UTF8.GetString(details.ResponseBodyInBytes);
				logger.LogDebug(details.OriginalException, "ElasticSearch message: {Method} {Uri} {Request} => {StatusCode} {Response}", details.HttpMethod, details.Uri, request, details.HttpStatusCode, response);
			}));
	}

	public async Task<IReadOnlyCollection<SourceDocument>> SearchAsync(string index, string query)
	{
		var response = await _client.SearchAsync<SourceDocument>(s => s
			.Index(index)
			.Query(q => q.SimpleQueryString(q => q.Query(query).Fields("text"))));
		return response.Documents;
	}

	public async Task IndexAsync(string index, IEnumerable<SourceDocument> documents)
	{
		await _client.IndexManyAsync(documents, index);
	}

	public async Task<IReadOnlyCollection<string>> GetIndicesAsync()
	{
		var response = await _client.Indices.GetAsync(new GetIndexRequest(Indices.All));
		return response.Indices.Keys.Select(x => x.ToString()).ToList();
	}

	public async Task DeleteIndexAsync(string index)
	{
		await _client.Indices.DeleteAsync(new DeleteIndexRequest(index) { RequestConfiguration = new RequestConfiguration() { ThrowExceptions = false }});
	}

	public async Task CreateIndexAsync(string index) => await _client.Indices.CreateAsync(index);

	private readonly ElasticsearchClient _client;
}
