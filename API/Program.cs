using Prometheus;
using TeslaGateway_PrometheusProxy;
using TeslaGateway_PrometheusProxy.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<TeslaGatewayMetricsService>();
builder.Services.Configure<LoginRequest>(builder.Configuration.GetSection("TeslaGateway"));
builder.Services.AddHttpClient(nameof(TeslaGatewayMetricsService), (_, client) => 
{
    client.BaseAddress = new Uri($"https://{builder.Configuration["TeslaGateway:Host"]}");
    // The Tesla Gateway only accepts a certain set of Host header values
    client.DefaultRequestHeaders.Host = "powerwall";
}).ConfigurePrimaryHttpMessageHandler(_ =>
{
    var handler = new HttpClientHandler();
    // Tesla Gateway serves a self-signed cert
    handler.ServerCertificateCustomValidationCallback = (_, _, _, _) => true;
    return handler;
}).UseHttpClientMetrics();

Metrics.DefaultRegistry.SetStaticLabels(new Dictionary<string, string>()
{
    { "TeslaGatewayHost", builder.Configuration["TeslaGateway:Host"] }
});

builder.Services.AddSingleton<CollectorRegistry>(Metrics.DefaultRegistry);

var app = builder.Build();

// Configure the HTTP request pipeline.
// Since we have no auth, go ahead and always use developer exception page.
app.UseDeveloperExceptionPage();
app.MapControllers();

app.Run();
