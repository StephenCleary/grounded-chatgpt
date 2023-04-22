namespace server.Data;

public sealed class ResourceDownloader
{
    public ResourceDownloader(IWebHostEnvironment webHostEnvironment)
    {
        _scratchPath = Path.Combine(webHostEnvironment.ContentRootPath, "scratch");
        Directory.CreateDirectory(_scratchPath);
    }

    private readonly string _scratchPath;

    private static readonly (string Title, string Uri)[] EtiquetteSources =
    {
    };
}
