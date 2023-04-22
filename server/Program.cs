using Elastic.Clients.Elasticsearch;
using server.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddLogging(logs => logs.AddSeq("http://seq:5341"));

builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

builder.Services.AddSingleton(new ElasticsearchClient(new Uri("http://elasticsearch:9200")));
builder.Services.AddSingleton<ResourceDownloader>();
builder.Services.AddSingleton<WeatherForecastService>();

var app = builder.Build();

app.UseStaticFiles();

app.UseRouting();

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();
