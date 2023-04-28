using Azure.AI.OpenAI;
using Elastic.Clients.Elasticsearch;
using Microsoft.Extensions.Options;
using server.Data.Elastic;

namespace server.Data;

public sealed class RetrieveThenRead
{
	public RetrieveThenRead(ElasticsearchClient elasticsearchClient, OpenAIClient openAIClient, IOptions<OpenAIClientOptions> aiOptions)
	{
		_elasticsearchClient = elasticsearchClient;
		_openAIClient = openAIClient;
		_aiOptions = aiOptions.Value;
	}

	public async Task<(string Result, decimal Cost)> RunAsync(string userQuery)
	{
		var searchResponse = await _elasticsearchClient.SearchAsync<SourceDocument>(s => s
			.Index("bible")
			.Query(q => q.SimpleQueryString(q => q.Query(userQuery))));

		var searchDocuments = searchResponse.Documents;
		if (searchDocuments.Count == 0)
			return ("{No search results}", 0);

		var prompt = string.Format(_template, userQuery, string.Join("\n", searchDocuments.Select(x => $"{x.Id}\t{x.Text}"))).Replace("\r\n", "\n");

		var chatResponse = await _openAIClient.GetCompletionsAsync(
			deploymentOrModelName: _aiOptions.ChatDeployment,
			new CompletionsOptions()
			{
				Prompts = { prompt },
				ChoicesPerPrompt = 1,
				Temperature = (float)0.7,
				MaxTokens = 800,
				NucleusSamplingFactor = (float)0.95,
				FrequencyPenalty = 0,
				PresencePenalty = 0,
				StopSequences = { "###" },
			});
		var chatCompletions = chatResponse.Value;
		var cost = 0.002M / 1000M * chatCompletions.Usage.TotalTokens;
		var choice = chatCompletions.Choices.FirstOrDefault();
		if (choice == null)
			return ("{No choices returned.}", cost);

		return (choice.Text, cost);
	}

	private const string _template =
	"""
	You are an intelligent assistant helping people with questions about the Bible.
	Answer the following question using only the data provided in the sources below.
	You may include multiple answers, but each answer may only use the data provided in the sources below.
	Each source has a name followed by tab and the actual information, always include the source name for each fact you use in the response.
	If you cannot answer using the sources below, say you don't know.
	Do not comment on unused sources.

	###
	Question: 'What does the Bible say about love?'

	Sources:
	John 3:16	For God so loved the world that He gave His only begotten son.
	Proverbs 17:17	A friend loves at all times, and a brother is born for adversity.
	John 15:12	My command is this: Love each other as I have loved you.
	Luke 6:31	Do to others as you would have them do to you.

	Answer:
	God loves us [John 3:16]. We are commanded to love others [John 15:12][Luke 6:31].

	###
	Question: '{0}?'

	Sources:
	{1}

	Answer:

	""";
	private readonly ElasticsearchClient _elasticsearchClient;
	private readonly OpenAIClient _openAIClient;
	private readonly OpenAIClientOptions _aiOptions;
}
