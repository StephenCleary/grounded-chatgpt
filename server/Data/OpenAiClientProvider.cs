using Azure;
using Azure.AI.OpenAI;

namespace server.Data;

public sealed class OpenAiClientProvider
{
	public OpenAIClient? Client { get; private set; }

	public void SetApiKey(string apiKey)
	{
		Client = string.IsNullOrEmpty(apiKey) ? null : new OpenAIClient(
			new Uri("https://clearyopenaitest.openai.azure.com/"),
			new AzureKeyCredential(apiKey),
			new OpenAIClientOptions()
			{
				Diagnostics =
				{
					IsLoggingContentEnabled = true,
					LoggedHeaderNames = { "openai-model", "openai-processing-ms" },
				},
			});
	}
}
