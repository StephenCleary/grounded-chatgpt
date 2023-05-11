using Azure;
using Azure.AI.OpenAI;
using Microsoft.Extensions.Options;

namespace server.Data;

public sealed class OpenAiClientProvider
{
	public OpenAIClient? Client { get; private set; }
	public IOptions<OpenAIOptions>? Options { get; private set; }

	public void SetOptions(IOptions<OpenAIOptions> options)
	{
		Options = options;
		if (string.IsNullOrEmpty(options.Value.Apikey) || string.IsNullOrEmpty(options.Value.Uri))
		{
			Client = null;
			return;
		}

		Client = new OpenAIClient(
			new Uri(options.Value.Uri),
			new AzureKeyCredential(options.Value.Apikey),
			new OpenAIClientOptions()
			{
				Diagnostics =
				{
					IsLoggingContentEnabled = true,
					LoggedContentSizeLimit = int.MaxValue,
					LoggedHeaderNames = { "openai-model", "openai-processing-ms" },
				},
			});
	}
}
