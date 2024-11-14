using System.Reflection;
using Azure.Monitor.OpenTelemetry.AspNetCore;
using Azure.Monitor.OpenTelemetry.Exporter;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using OpenTelemetry;
using OpenTelemetry.Instrumentation.AspNetCore;
using OpenTelemetry.Instrumentation.Http;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Prometheus;
using SolarGateway_PrometheusProxy;
using SolarGateway_PrometheusProxy.Models;
using SolarGateway_PrometheusProxy.Services;
using SolarGateway_PrometheusProxy.Support;

var builder = WebApplication.CreateBuilder(args);

// Optional configuration override file
builder.Configuration.AddJsonFile("custom.json", optional: true);

// Add services to the container.
var services = builder.Services;

// Telemetry
services.AddMetrics();
bool useAzureMonitor = !string.IsNullOrWhiteSpace(builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"]);
bool useOtlpExporter = !string.IsNullOrWhiteSpace(builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]);
builder.Logging.AddOpenTelemetry(o =>
{
    o.ParseStateValues =
       o.IncludeFormattedMessage =
       o.IncludeScopes = true;
});
var otel = services.AddOpenTelemetry()
    .ConfigureResource(rb =>
    {
        _ = rb.AddService(
                builder.Environment.ApplicationName,
                serviceNamespace: "DrEsteban",
                serviceVersion: Assembly.GetExecutingAssembly().GetName().Version?.ToString())
            .AddAttributes([KeyValuePair.Create<string, object>("ASPNETCORE_ENVIRONMENT", builder.Environment.EnvironmentName)])
            .AddContainerDetector()
            .AddEnvironmentVariableDetector()
            .AddHostDetector()
            .AddOperatingSystemDetector()
            .AddProcessDetector()
            .AddProcessRuntimeDetector()
            .AddTelemetrySdk();
    })
    .WithLogging()
    .WithTracing(o =>
    {
        o.AddProcessor<MyHttpTraceActivityProcessor>()
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .SetErrorStatusOnException();
    })
    .WithMetrics(o =>
    {
        o.AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddProcessInstrumentation()
            .AddRuntimeInstrumentation();
    });
if (useAzureMonitor)
{
    otel.UseAzureMonitor();
}
if (useOtlpExporter)
{
    otel.UseOtlpExporter();
}
// Instrumentation customizations
services.Configure<AspNetCoreTraceInstrumentationOptions>(options =>
{
    options.RecordException = true;
});
services.Configure<HttpClientTraceInstrumentationOptions>(options =>
{
    options.RecordException = true;
});

// Prometheus
// TODO: Consider refactoring to use OpenTelemetry Prometheus exporter instead of Prometheus client.
//       Would require re-writing solar metrics as observable .NET Meters.
services.AddSingleton<CollectorRegistry>(Metrics.DefaultRegistry);

// Http
var responseCacheConfiguration = builder.Configuration.Get<ResponseCacheConfiguration>() ?? new();
services.AddSingleton<IOptions<ResponseCacheConfiguration>>(Options.Create(responseCacheConfiguration));
services.AddControllers(c =>
{
    var profile = new CacheProfile()
    {
        Duration = responseCacheConfiguration.ResponseCacheDurationSeconds
    };

    if (profile.Duration <= 0)
    {
        profile.Duration = null;
        profile.NoStore = true;
    }

    c.CacheProfiles.Add("default", profile);
});
services.AddMemoryCache();
services.AddHttpContextAccessor();
services.AddTransient<OutboundHttpClientLogger>();

// Collectors:
// Tesla
if (builder.Configuration.GetValue<bool>("TeslaGateway:Enabled"))
{
    services.Configure<TeslaLoginRequest>(builder.Configuration.GetSection("TeslaGateway"));
    services.Configure<TeslaConfiguration>(builder.Configuration.GetSection("TeslaGateway"));
    services.AddHttpClient<TeslaGatewayMetricsService>(client =>
        {
            client.BaseAddress = new Uri($"https://{builder.Configuration["TeslaGateway:Host"]}");
            // The Tesla Gateway only accepts a certain set of Host header values
            client.DefaultRequestHeaders.Host = "powerwall";
        })
        .ConfigurePrimaryHttpMessageHandler(_ =>
        {
            var handler = new HttpClientHandler();
            // Tesla Gateway serves a self-signed cert
            handler.ServerCertificateCustomValidationCallback = (_, _, _, _) => true;
            return handler;
        })
        .UseHttpClientMetrics()
        .RemoveAllLoggers()
        .AddLogger<OutboundHttpClientLogger>();
    services.AddScoped<IMetricsService, TeslaGatewayMetricsService>();
}

// Enphase
if (builder.Configuration.GetValue<bool>("Enphase:Enabled"))
{
    services.AddHttpClient<EnphaseMetricsService>(client =>
        {
            client.BaseAddress = new Uri($"http://{builder.Configuration["Enphase:Host"]}");
        })
        .UseHttpClientMetrics()
        .RemoveAllLoggers()
        .AddLogger<OutboundHttpClientLogger>();
    services.AddScoped<IMetricsService, EnphaseMetricsService>();
}

// Build app
await using var app = builder.Build();

// Configure the HTTP request pipeline.
// Since we have no auth, go ahead and always use developer exception page.
app.UseDeveloperExceptionPage();
app.UseResponseCaching();
app.MapControllers();

await app.RunAsync();