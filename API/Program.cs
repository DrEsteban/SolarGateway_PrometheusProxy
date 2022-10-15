using Prometheus;
using SolarGateway_PrometheusProxy;
using SolarGateway_PrometheusProxy.MetricServices;
using SolarGateway_PrometheusProxy.Models;

var builder = WebApplication.CreateBuilder(args);

// Add common services to the container.
builder.Services.AddControllers();
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<CollectorRegistry>(Metrics.DefaultRegistry);

var staticLabels = new Dictionary<string, string>();

// Tesla
if (builder.Configuration.GetValue<bool>("TeslaGateway:Enabled"))
{
    builder.Services.AddSingleton<IMetricsService, TeslaGatewayMetricsService>();
    builder.Services.Configure<TeslaLoginRequest>(builder.Configuration.GetSection("TeslaGateway"));
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

    staticLabels.Add("TeslaGatewayHost", builder.Configuration["TeslaGateway:Host"]);
}

// Enphase
if (builder.Configuration.GetValue<bool>("Enphase:Enabled"))
{
    builder.Services.AddSingleton<IMetricsService, EnphaseMetricsService>();
    builder.Services.AddHttpClient(nameof(EnphaseMetricsService), (_, client) =>
    {
        client.BaseAddress = new Uri($"http://{builder.Configuration["Enphase:Host"]}");
    }).UseHttpClientMetrics();

    staticLabels.Add("EnphaseHost", builder.Configuration["Enphase:Host"]);
}

Metrics.DefaultRegistry.SetStaticLabels(staticLabels);

// Build app
var app = builder.Build();

// Configure the HTTP request pipeline.
// Since we have no auth, go ahead and always use developer exception page.
app.UseDeveloperExceptionPage();
app.MapControllers();

app.Run();