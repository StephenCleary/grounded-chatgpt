using Azure.AI.OpenAI;
using Elastic.Clients.Elasticsearch;
using server.Data.Elastic;

namespace server.Data;

/// <summary>
/// Approach:
/// - Use Completion to generate the search query from the user's question.
/// - Run the search query against the database.
/// - Using the query results as sources, Complete the original question with an answer.
/// </summary>
public sealed class CompleteRetrieveRead
{
	public CompleteRetrieveRead(ElasticsearchClient elasticsearchClient, OpenAiClientProvider openAiClientProvider, ILogger<CompleteRetrieveRead> logger)
	{
		_elasticsearchClient = elasticsearchClient;
		_openAiClientProvider = openAiClientProvider;
		_logger = logger;
	}

	public async Task<(string Result, decimal Cost)> RunAsync(string userQuery)
	{
		if (_openAiClientProvider.Client == null)
			return ("{No API key}", 0);

		var searchQueryCompletionResponse = await _openAiClientProvider.Client.GetCompletionsAsync(
			deploymentOrModelName: "gpt35t",
			new CompletionsOptions()
			{
				Prompts = { _queryTemplate.Template(new { question = userQuery }) },
				ChoicesPerPrompt = 1,
				Temperature = 0,
				MaxTokens = 32,
				StopSequences = { "\n" },
			});

		// TODO: track cost and other details
		var searchQuery = searchQueryCompletionResponse.Value.Choices.FirstOrDefault()?.Text;
		if (searchQuery == null)
		{
			_logger.LogWarning("Unable to determine query for user input {userQuery}", userQuery);
			searchQuery = userQuery;
		}

		var searchResponse = await _elasticsearchClient.SearchAsync<BibleDocument>(s => s
			.Index("bible")
			.Query(q => q.SimpleQueryString(q => q.Query(searchQuery))));

		var searchDocuments = searchResponse.Documents;
		if (searchDocuments.Count == 0)
			return ("{No search results}", 0);

		var prompt = _promptTemplate.Template(new { sources = string.Join("\n", searchDocuments.Select(x => $"{x.Id}\t{x.Text}")) });

		var chatResponse = await _openAiClientProvider.Client.GetChatCompletionsAsync(
			deploymentOrModelName: "gpt35t",
			new ChatCompletionsOptions()
			{
				Messages =
				{
					new(ChatRole.System, prompt),
					new(ChatRole.User, userQuery),
				},
				ChoicesPerPrompt = 1,
				Temperature = (float)0.7,
				MaxTokens = 1024,
				NucleusSamplingFactor = (float)0.95,
				FrequencyPenalty = 0,
				PresencePenalty = 0,
			});
		var chatCompletions = chatResponse.Value;
		var cost = 0.002M / 1000M * chatCompletions.Usage.TotalTokens;
		var choice = chatCompletions.Choices.FirstOrDefault();
		if (choice == null)
			return ("{No choices returned.}", cost);

		var result = choice.Message.Content;
		if (result.StartsWith("According to the sources provided, "))
			result = result["According to the sources provided, ".Length..];

		return (choice.Message.Content, cost);
	}

	private const string _queryTemplate =
	"""
	Below is a question asked by the user that needs to be answered by searching.
	Generate a search query based on names and concepts extracted from the question.

	Question:
	{question}

	Search query:
	
	""";

	private const string _promptTemplate =
	"""
	You are an intelligent assistant helping people with questions about the Bible.
	Answer the following question using only the data provided in the sources below.
	You may include multiple answers, but each answer may only use the data provided in the sources below.
	Each source has a name followed by tab and the actual information. Always include the source name for each fact you use in the response. Each source has a name followed by colon and the actual information, always include the source name for each fact you use in the response. Use square brakets to reference the source, e.g. [info1.txt]. Don't combine sources; list each source separately, e.g. [info1.txt][info2.pdf].
	If you cannot answer using the sources below, say you don't know. Do not generate answers that don't use the sources below. If asking a clarifying question to the user would help, ask the question.
	Do not comment on unused sources.

	Sources:
	{sources}

	""";

	private readonly ElasticsearchClient _elasticsearchClient;
	private readonly OpenAiClientProvider _openAiClientProvider;
	private readonly ILogger<CompleteRetrieveRead> _logger;
}
