using Azure.AI.OpenAI;

namespace server.Data;

/// <summary>
/// Approach:
/// - Use Completion to generate the search query from the user's question.
/// - Run the search query against the database.
/// - Using the query results as sources, Complete the original question with an answer.
/// </summary>
public sealed class RetrievalAugmentedGeneration
{
	public RetrievalAugmentedGeneration(ElasticsearchService elasticsearchService, OpenAiClientProvider openAiClientProvider, ILogger<RetrievalAugmentedGeneration> logger)
	{
		_elasticsearchService = elasticsearchService;
		_openAiClientProvider = openAiClientProvider;
		_logger = logger;
	}

	public async Task<(string Result, decimal Cost, IReadOnlyDictionary<string, (string Uri, string Name)> References)> RunAsync(string searchIndex, string role, string question)
	{
		if (_openAiClientProvider.Client == null)
			return ("{No API key}", 0, EmptyReferences);

        question = question.Replace("\r\n", "\n");

		// Extract search keywords from the user's question. This is necessary because we're doing lexical search instead of semantic or vector.
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

		// Search ElasticSearch using the extracted search keywords.
		var searchDocuments = await _elasticsearchService.SearchAsync(searchIndex, searchQuery);
		if (searchDocuments.Count == 0)
			return ("I don't know.", cost, EmptyReferences);

		// Map simple reference numbers to the source URIs.
		Dictionary<string, (string Uri, string Name)> referenceMap = searchDocuments
			.Select((sourceDocument, i) => (sourceDocument, i))
			.ToDictionary(x => $"[{x.i + 1}]", x => (x.sourceDocument.Uri, x.sourceDocument.Name));

		// Use the Role and the search Results to build a system prompt defining how the AI should respond to the user.
		var searchDocumentsUsed = searchDocuments.AsEnumerable();
		var prompt = _promptTemplate.Template(new { role, sources = string.Join("\n", searchDocumentsUsed.Select((x, i) => $"{i + 1}\t{x.Text}")) });
		while (_openAiClientProvider.Options!.Value.ChatModel.TokenCount(prompt) > MaximumSystemPromptTokenLength)
		{
			searchDocumentsUsed = searchDocumentsUsed.Take(searchDocumentsUsed.Count() - 1);
			prompt = _promptTemplate.Template(new { role, sources = string.Join("\n", searchDocumentsUsed.Select((x, i) => $"{i + 1}\t{x.Text}")) });
		}

		// Send the system prompt and the user's question to the AI.
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
				MaxTokens = RequestedResponseTokenLength,
				NucleusSamplingFactor = (float)0.95,
				FrequencyPenalty = 0,
				PresencePenalty = 0,
			});
		var chatCompletions = chatResponse.Value;
		cost += _openAiClientProvider.Options.Value.ChatModel.Cost(chatCompletions.Usage.TotalTokens);
		var choice = chatCompletions.Choices.FirstOrDefault();
		if (choice == null)
			return ("I don't know.", cost, referenceMap);

		return (choice.Message.Content, cost, referenceMap);
	}

	public static readonly Dictionary<string, (string Uri, string Name)> EmptyReferences = new();

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
	Each Reference has a number followed by tab and then its data.
	Use square brakets to indicate which Reference was used, e.g. [7]
	Don't combine References; list each Reference separately, e.g. [1][2]
	If you cannot answer using the References below, say you don't know. Only provide answers that include at least one Reference name.
	If asking a clarifying question to the user would help, ask the question.
	Do not comment on unused References.

	###	References:
	{sources}

	""";

	// The model limit is 8k, so reserving 6k for the system prompt leaves 1k each for the user's question and the model's response.
	// This generally allows for ~5 search responses, since they are limited to 1k each.
	private const int MaximumSystemPromptTokenLength = 1024 * 6;
	private const int RequestedResponseTokenLength = 1024;

	private readonly ElasticsearchService _elasticsearchService;
	private readonly OpenAiClientProvider _openAiClientProvider;
	private readonly ILogger<RetrievalAugmentedGeneration> _logger;
}
