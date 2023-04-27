using Microsoft.Extensions.Options;

namespace server.Data;

public sealed class OpenAIClientOptions : IOptions<OpenAIClientOptions>
{
	public string Uri { get; set; } = null!;
	public string Apikey { get; set; } = null!;

	OpenAIClientOptions IOptions<OpenAIClientOptions>.Value => this;
}
