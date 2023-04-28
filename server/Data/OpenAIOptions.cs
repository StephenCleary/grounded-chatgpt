using Microsoft.Extensions.Options;

namespace server.Data;

public sealed class OpenAIOptions : IOptions<OpenAIOptions>
{
	public string Uri { get; set; } = null!;
	public string Apikey { get; set; } = null!;
	public string ChatDeployment { get; set; } = null!;
	public string ExtractDeployment { get; set; } = null!;
	public Model ChatModel => Model.Parse(ChatDeployment);
	public Model ExtractModel => Model.Parse(ExtractDeployment);

	OpenAIOptions IOptions<OpenAIOptions>.Value => this;
}
