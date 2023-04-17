using Microsoft.AspNetCore.Mvc;
using Prometheus;
using SolarGateway_PrometheusProxy;
using SolarGateway_PrometheusProxy.MetricServices;
using SolarGateway_PrometheusProxy.Models;

var builder = WebApplication.CreateBuilder(args);

// Optional configuration override file
builder.Configuration.AddJsonFile("custom.json", optional: true);

// Add common services to the container.
builder.Services.AddControllers(c =>
{
    var profile = new CacheProfile()
    {
        Duration = builder.Configuration.GetValue<int>("ResponseCacheDurationSeconds")
    };

    if (profile.Duration <= 0)
    {
        profile.Duration = null;
        profile.NoStore = true;
    }

    c.CacheProfiles.Add("default", profile);
});
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<CollectorRegistry>(Metrics.DefaultRegistry);

// Tesla
if (builder.Configuration.GetValue<bool>("TeslaGateway:Enabled"))
{
    builder.Services.AddScoped<IMetricsService, TeslaGatewayMetricsService>();
    builder.Services.Configure<TeslaLoginRequest>(builder.Configuration.GetSection("TeslaGateway"));
    builder.Services.Configure<TeslaConfiguration>(builder.Configuration.GetSection("TeslaGateway"));
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
}

// Enphase
if (builder.Configuration.GetValue<bool>("Enphase:Enabled"))
{
    builder.Services.AddScoped<IMetricsService, EnphaseMetricsService>();
    builder.Services.AddHttpClient(nameof(EnphaseMetricsService), (_, client) =>
    {
        client.BaseAddress = new Uri($"http://{builder.Configuration["Enphase:Host"]}");
    }).UseHttpClientMetrics();
}

// Build app
var app = builder.Build();

// Configure the HTTP request pipeline.
// Since we have no auth, go ahead and always use developer exception page.
app.UseDeveloperExceptionPage();
app.UseResponseCaching();
app.MapControllers();

app.Run();