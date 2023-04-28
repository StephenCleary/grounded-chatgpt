using SharpToken;

namespace server.Data;

public abstract class Model
{
	public static Model Parse(string name) => name switch
	{
		Gpt35TurboName => Gpt35Turbo,
		_ => throw new NotImplementedException(),
	};
	public static readonly Model Gpt35Turbo = new Gpt35TurboModel();

	public string Name { get; }
	public abstract int TokenCount(string text);
	public abstract decimal Cost(int tokenCount);

	private Model(string name)
	{
		Name = name;
	}

	private sealed class Gpt35TurboModel : Model
	{
		public Gpt35TurboModel()
			: base(Gpt35TurboName)
		{
		}

		public override decimal Cost(int tokenCount)
		{
			// https://azure.microsoft.com/en-us/pricing/details/cognitive-services/openai-service/
			return tokenCount * 0.002M / 1000M;
		}

		public override int TokenCount(string text) => cl100k.Encode(text).Count;
	}

	private const string Gpt35TurboName = "gpt-35-turbo";
	private static readonly GptEncoding cl100k = GptEncoding.GetEncoding("cl100k_base");
}
