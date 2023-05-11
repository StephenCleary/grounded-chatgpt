namespace server.Data;

public interface IChatStrategy
{
	Task<(string Result, decimal Cost, IReadOnlyDictionary<string, (string Uri, string Name)> References)> RunAsync(string searchIndex, string role, string question);
	public static readonly Dictionary<string, (string Uri, string Name)> EmptyReferences = new();
}
