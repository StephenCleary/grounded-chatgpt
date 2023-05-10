using Microsoft.Extensions.Azure;
using Nito.Logging;
using server.Data;

var builder = WebApplication.CreateBuilder(args);

// Log to Seq
builder.Services.AddLogging(logs => logs.AddSeq());
builder.Services.AddSingleton<AzureEventSourceLogForwarder>();

// Framework types
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

// Application types
builder.Services.AddSingleton<ElasticsearchService>();
builder.Services.AddSingleton<ResourceIndexer>();
builder.Services.AddSingleton<OpenAiClientProvider>();
builder.Services.AddSingleton<GlobalCostTracker>();
builder.Services.AddTransient<CompleteRetrieveRead>();

var app = builder.Build();

// Logging: include an InstanceId representing this instance of the application.
using var _ = app.Services.GetRequiredService<ILogger<Program>>().BeginDataScope(("InstanceId", Guid.NewGuid().ToString("N")));

// Logging: forward Azure logs from its event source system to the .NET ILogger.
app.Services.GetRequiredService<AzureEventSourceLogForwarder>().Start();

// Framework setup
app.UseStaticFiles();
app.UseRouting();
app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();
