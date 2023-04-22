using Azure.Core.Diagnostics;
using Elastic.Clients.Elasticsearch;
using Microsoft.Extensions.Azure;
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
        app.Services.GetRequiredService<ILogger<ElasticsearchClient>>().LogInformation(details.OriginalException, "ElasticSearch message: {Method} {Uri} {Request} => {StatusCode} {Response}", details.HttpMethod, details.Uri, request, details.HttpStatusCode, response);
    })));
builder.Services.AddSingleton<ResourceDownloader>();
builder.Services.AddSingleton<ResourceIndexer>();
builder.Services.AddSingleton<WeatherForecastService>();

app = builder.Build();
app.Services.GetRequiredService<AzureEventSourceLogForwarder>().Start();

app.UseStaticFiles();

app.UseRouting();

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();
