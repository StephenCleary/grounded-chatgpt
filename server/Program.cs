using Azure;
using Azure.AI.OpenAI;
using Elastic.Clients.Elasticsearch;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Options;
using Nito.Logging;
using server.Data;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddLogging(logs => logs.AddSeq());
builder.Services.AddSingleton<AzureEventSourceLogForwarder>();

builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

WebApplication app = null!;
builder.Services.AddSingleton(new ElasticsearchClient(new ElasticsearchClientSettings()
    .ThrowExceptions()
    .DisableDirectStreaming()
    .OnRequestCompleted(details =>
    {
        var request = details.RequestBodyInBytes == null ? null : Encoding.UTF8.GetString(details.RequestBodyInBytes);
        var response = details.ResponseBodyInBytes == null ? null : Encoding.UTF8.GetString(details.ResponseBodyInBytes);
        app.Services.GetRequiredService<ILogger<ElasticsearchClient>>().LogDebug(details.OriginalException, "ElasticSearch message: {Method} {Uri} {Request} => {StatusCode} {Response}", details.HttpMethod, details.Uri, request, details.HttpStatusCode, response);
    })));
builder.Services.AddSingleton<ResourceIndexer>();
builder.Services.AddSingleton<OpenAiClientProvider>();
builder.Services.AddSingleton<GlobalCostTracker>();
builder.Services.AddTransient<CompleteRetrieveRead>();

app = builder.Build();
using var _ = app.Services.GetRequiredService<ILogger<Program>>().BeginDataScope(("InstanceId", Guid.NewGuid().ToString("N")));
app.Services.GetRequiredService<AzureEventSourceLogForwarder>().Start();

app.UseStaticFiles();

app.UseRouting();

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();
