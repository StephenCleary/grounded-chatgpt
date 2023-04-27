using Blazored.SessionStorage;

namespace server.Data;

public sealed class CostTracker
{
	private readonly GlobalCostTracker _globalCostTracker;
    private readonly ISessionStorageService _sessionStorageService;

    public CostTracker(GlobalCostTracker globalCostTracker, ISessionStorageService sessionStorageService)
	{
		_globalCostTracker = globalCostTracker;
        _sessionStorageService = sessionStorageService;
    }

	public decimal? Cost { get; private set; }
	public decimal GlobalCost => _globalCostTracker.Cost;

	public async Task AddAsync(decimal cost)
	{
		Cost += cost;
		_globalCostTracker.Add(cost);
		await _sessionStorageService.SetItemAsync("cost", Cost);
	}

	public async Task RefreshAsync()
	{
		Cost = await _sessionStorageService.GetItemAsync<decimal>("cost");
	}

	public static decimal CostOfTokens(string model, int tokens)
	{
		if (!TokenCostByModel.TryGetValue(model, out var multiplier))
			throw new InvalidOperationException($"Unknown cost for model {model}");
		return multiplier * tokens;
	}

	// https://azure.microsoft.com/en-us/pricing/details/cognitive-services/openai-service/
	private static readonly Dictionary<string, decimal> TokenCostByModel = new()
	{
		{ "gpt-3.5-turbo", 0.002M / 1000M },
	};
}
