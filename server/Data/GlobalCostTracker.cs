using Nito.Disposables;

namespace server.Data;

public sealed class GlobalCostTracker
{
	private readonly object _mutex = new();
	private decimal _cost;

	public Decimal Cost { get => _cost; }

	public void Add(Decimal cost)
	{
		lock (_mutex) { _cost += cost; }
		OnChanged?.Invoke();
	}

    public IDisposable Subscribe(Action onChanged)
    {
		OnChanged += onChanged;
		return Disposable.Create(() => OnChanged -= onChanged);
    }

    private Action? OnChanged;
}
