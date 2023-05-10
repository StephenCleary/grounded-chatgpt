namespace server.Data;

public static class Globals
{
    public static HttpClient HttpClient { get; } = new(new SocketsHttpHandler
    {
        PooledConnectionLifetime = TimeSpan.FromMinutes(2),
    });
}
