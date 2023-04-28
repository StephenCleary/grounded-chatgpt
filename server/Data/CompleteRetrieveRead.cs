using Azure.AI.OpenAI;
using Elastic.Clients.Elasticsearch;
using Microsoft.Extensions.Options;
using server.Data.Elastic;
using SharpToken;

namespace server.Data;

/// <summary>
/// Approach:
/// - Use Completion to generate the search query from the user's question.
/// - Run the search query against the database.
/// - Using the query results as sources, Complete the original question with an answer.
/// </summary>
public sealed class CompleteRetrieveRead
{
	public CompleteRetrieveRead(ElasticsearchClient elasticsearchClient, OpenAIClient openAIClient, IOptions<OpenAIOptions> aiOptions, ILogger<CompleteRetrieveRead> logger)
	{
		_elasticsearchClient = elasticsearchClient;
		_openAIClient = openAIClient;
		_logger = logger;
		_aiOptions = aiOptions.Value;
	}

	public async Task<(string Result, decimal Cost)> RunAsync(string userQuery)
	{
		userQuery = userQuery.Replace("\r\n", "\n");
		var userQueryTokenCount = GptEncoding.GetEncoding("cl100k_base").Encode(userQuery).Count;

		var searchQueryCompletionResponse = await _openAIClient.GetCompletionsAsync(
			deploymentOrModelName: _aiOptions.ExtractDeployment,
			new CompletionsOptions()
			{
				Prompts = { _queryTemplate.Template(new { question = userQuery }) },
				ChoicesPerPrompt = 1,
				Temperature = 0,
				MaxTokens = 32,
				StopSequences = { "\n" },
			});
		var cost = CostTracker.CostOfTokens(_aiOptions.ExtractDeployment, searchQueryCompletionResponse.Value.Usage.TotalTokens);

		var searchQuery = searchQueryCompletionResponse.Value.Choices.FirstOrDefault()?.Text;
		if (searchQuery == null)
		{
			_logger.LogWarning("Unable to determine query for user input {userQuery}", userQuery);
			searchQuery = userQuery;
		}

		var searchResponse = await _elasticsearchClient.SearchAsync<SourceDocument>(s => s
			.Index("bible")
			.Query(q => q.SimpleQueryString(q => q.Query(searchQuery))));

		var searchDocuments = searchResponse.Documents;
		if (searchDocuments.Count == 0)
			return ("I don't know.", 0);

		var prompt = _promptTemplate.Template(new { sources = string.Join("\n", searchDocuments.Select(x => $"{x.Id}\t{x.Text}")) });

		var chatResponse = await _openAIClient.GetChatCompletionsAsync(
			deploymentOrModelName: _aiOptions.ChatDeployment,
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
		cost += CostTracker.CostOfTokens(_aiOptions.ChatDeployment, chatCompletions.Usage.TotalTokens);
		var choice = chatCompletions.Choices.FirstOrDefault();
		if (choice == null)
			return ("I don't know.", cost);

		return (choice.Message.Content, cost);
	}

	private const string _queryTemplate =
	"""
	Below is a question asked by the user that needs to be answered by searching.
	Generate a search query based on names and concepts extracted from the question.

	### Question:
	{question}

	### Search query:
	
	""";

	private const string _promptTemplate =
	"""
	You are an intelligent assistant helping people with questions about the Bible.
	Answer the following question. You may include multiple answers, but each answer may only use the data provided in the References below.
	Each Reference has a name followed by tab and then its data.
	Use square brakets to indicate which Reference was used, e.g. [John 3:16]
	Don't combine References; list each Reference separately, e.g. [Genesis 1:1][John 3:16]
	If you cannot answer using the References below, say you don't know. Only provide answers that include at least one Reference name.
	If asking a clarifying question to the user would help, ask the question.
	Do not comment on unused References.

	###	References:
	{sources}

	""";

	private readonly ElasticsearchClient _elasticsearchClient;
	private readonly OpenAIClient _openAIClient;
	private readonly ILogger<CompleteRetrieveRead> _logger;
	private readonly OpenAIOptions _aiOptions;
}
