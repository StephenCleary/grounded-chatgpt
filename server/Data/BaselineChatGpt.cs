using Azure.AI.OpenAI;

namespace server.Data;

/// <summary>
/// Approach:
/// - Just send the user query to ChatGPT, with an optional role.
/// </summary>
public sealed class BaselineChatGpt
{
	public BaselineChatGpt(OpenAiClientProvider openAiClientProvider)
	{
		_openAiClientProvider = openAiClientProvider;
	}

	public async Task<(string Result, decimal Cost)> RunAsync(string? role, string question)
	{
		// Just shoot it off to ChatGPT.

		if (_openAiClientProvider.Client == null)
			return ("{No API key}", 0);

		var chatOptions = new ChatCompletionsOptions()
		{
			Temperature = (float)0.7,
			MaxTokens = RequestedResponseTokenLength,
			NucleusSamplingFactor = (float)0.95,
			FrequencyPenalty = 0,
			PresencePenalty = 0,
		};
		if (role != null)
			chatOptions.Messages.Add(new ChatMessage(ChatRole.System, role));
		chatOptions.Messages.Add(new ChatMessage(ChatRole.User, question));

		var chatResponse = await _openAiClientProvider.Client.GetChatCompletionsAsync(deploymentOrModelName: _openAiClientProvider.Options!.Value.ChatDeployment, chatOptions);
		var chatCompletions = chatResponse.Value;

		var cost = _openAiClientProvider.Options.Value.ChatModel.Cost(chatCompletions.Usage.TotalTokens);
		var choice = chatCompletions.Choices.FirstOrDefault();
		if (choice == null)
			return ("I don't know.", cost);

		return (choice.Message.Content, cost);
	}

	private readonly OpenAiClientProvider _openAiClientProvider;

	private const int RequestedResponseTokenLength = 1024;
}
