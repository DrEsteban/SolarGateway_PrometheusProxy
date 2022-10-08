using Prometheus;
using TeslaGateway_PrometheusProxy;
using TeslaGateway_PrometheusProxy.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddMemoryCache();
builder.Services.Configure<LoginRequest>(builder.Configuration.GetSection("TeslaGateway"));
builder.Services.AddHttpClient(nameof(TeslaGatewayMetricsService), (_, client) => 
{
    client.BaseAddress = new Uri($"https://{builder.Configuration["TeslaGateway:Host"]}"); 
    client.DefaultRequestHeaders.Host = "powerwall";
}).ConfigurePrimaryHttpMessageHandler(_ =>
{
    var handler = new HttpClientHandler();
    // Tesla Gateway serves a self-signed cert
    handler.ServerCertificateCustomValidationCallback = (_, _, _, _) => true;
    return handler;
});
builder.Services.AddSingleton<TeslaGatewayMetricsService>();
builder.Services.AddSingleton<CollectorRegistry>(_ =>
{
    var registry = Metrics.NewCustomRegistry();
    registry.SetStaticLabels(new Dictionary<string, string>()
    {
        { "Host", builder.Configuration["TeslaGateway:Host"] }
    });
    return registry;
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.MapControllers();

app.Run();
