using System.Net.Http.Headers;
using Hello.AspNetCoreMcp.McpServer.Tools;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddMcpServer()
    .WithHttpTransport()
    .WithTools<WeatherTools>();

// Configure HttpClientFactory for weather.gov API
builder.Services.AddHttpClient("WeatherApi", client =>
{
    client.BaseAddress = new Uri("https://api.weather.gov");
    client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("weather-tool", "1.0"));
});
builder.Services.AddHttpClient("SimpleWeather", client =>
    client.BaseAddress = new("https://apiservice"));

var app = builder.Build();
app.MapDefaultEndpoints();
app.MapMcp();

app.Run();
