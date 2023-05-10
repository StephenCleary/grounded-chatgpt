﻿using Azure.AI.OpenAI;

namespace server.Data;

/// <summary>
/// Approach:
/// - Use Completion to generate the search query from the user's question.
/// - Run the search query against the database.
/// - Using the query results as sources, Complete the original question with an answer.
/// </summary>
public sealed class CompleteRetrieveRead
{
	public CompleteRetrieveRead(ElasticsearchService elasticsearchService, OpenAiClientProvider openAiClientProvider, ILogger<CompleteRetrieveRead> logger)
	{
		_elasticsearchService = elasticsearchService;
		_openAiClientProvider = openAiClientProvider;
		_logger = logger;
	}

	public async Task<(string Result, decimal Cost)> RunAsync(string searchIndex, string role, string question)
	{
		if (_openAiClientProvider.Client == null)
			return ("{No API key}", 0);

        question = question.Replace("\r\n", "\n");

		var searchQueryCompletionResponse = await _openAiClientProvider.Client.GetCompletionsAsync(
			deploymentOrModelName: _openAiClientProvider.Options!.Value.ExtractDeployment,
			new CompletionsOptions()
			{
				Prompts = { _queryTemplate.Template(new { question }) },
				ChoicesPerPrompt = 1,
				Temperature = 0,
				MaxTokens = 32,
				StopSequences = { "\n" },
			});
		var cost = _openAiClientProvider.Options.Value.ExtractModel.Cost(searchQueryCompletionResponse.Value.Usage.TotalTokens);

		var searchQuery = searchQueryCompletionResponse.Value.Choices.FirstOrDefault()?.Text;
		if (searchQuery == null)
		{
			_logger.LogWarning("Unable to determine query for user input {userQuery}", question);
			searchQuery = question;
		}

		var searchDocuments = await _elasticsearchService.SearchAsync(searchIndex, searchQuery);
		if (searchDocuments.Count == 0)
			return ("I don't know.", 0);

		var prompt = _promptTemplate.Template(new { role, sources = string.Join("\n", searchDocuments.Select(x => $"{x.Id}\t{x.Text}")) });

		var chatResponse = await _openAiClientProvider.Client.GetChatCompletionsAsync(
			deploymentOrModelName: _openAiClientProvider.Options!.Value.ChatDeployment,
            new ChatCompletionsOptions()
			{
				Messages =
				{
					new(ChatRole.System, prompt),
					new(ChatRole.User, question),
				},
				ChoicesPerPrompt = 1,
				Temperature = (float)0.7,
				MaxTokens = 1024,
				NucleusSamplingFactor = (float)0.95,
				FrequencyPenalty = 0,
				PresencePenalty = 0,
			});
		var chatCompletions = chatResponse.Value;
		cost += _openAiClientProvider.Options.Value.ChatModel.Cost(chatCompletions.Usage.TotalTokens);
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
	{role}
	Answer the following question. You may include multiple answers, but each answer may only use the data provided in the References below.
	Each Reference has a name followed by tab and then its data.
	Use square brakets to indicate which Reference was used, e.g. [ABC123]
	Don't combine References; list each Reference separately, e.g. [ABC123][DEF456]
	If you cannot answer using the References below, say you don't know. Only provide answers that include at least one Reference name.
	If asking a clarifying question to the user would help, ask the question.
	Do not comment on unused References.

	###	References:
	{sources}

	""";

	private readonly ElasticsearchService _elasticsearchService;
	private readonly OpenAiClientProvider _openAiClientProvider;
	private readonly ILogger<CompleteRetrieveRead> _logger;
}
