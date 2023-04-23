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

	public Decimal? Cost { get; private set; }
	public Decimal GlobalCost => _globalCostTracker.Cost;

	public async Task AddAsync(Decimal cost)
	{
		Cost += cost;
		_globalCostTracker.Add(cost);
		await _sessionStorageService.SetItemAsync("cost", Cost);
	}

	public async Task RefreshAsync()
	{
		Cost = await _sessionStorageService.GetItemAsync<decimal>("cost");
	}
}
