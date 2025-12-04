using System.Reflection;
using Azure.Monitor.OpenTelemetry.AspNetCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Instrumentation.AspNetCore;
using OpenTelemetry.Instrumentation.Http;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Prometheus;
using SolarGateway_PrometheusProxy;
using SolarGateway_PrometheusProxy.Configuration;
using SolarGateway_PrometheusProxy.HealthChecks;
using SolarGateway_PrometheusProxy.Services;
using SolarGateway_PrometheusProxy.Support;

// Create the builder:
var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;
var environment = builder.Environment;
var logging = builder.Logging;
var services = builder.Services;

// Optional configuration override file
configuration.AddJsonFile("custom.json", optional: true);

// Config objects needed during initialization
var responseCacheConfiguration = configuration.Get<ResponseCacheConfiguration>() ?? new();
// Begin adding services to the container:
// Telemetry
services.AddMetrics();
bool useAzureMonitor = !string.IsNullOrWhiteSpace(configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"]);
bool useOtlpExporter = !string.IsNullOrWhiteSpace(configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]);
logging.AddOpenTelemetry(o =>
{
    o.ParseStateValues =
       o.IncludeFormattedMessage =
       o.IncludeScopes = true;
});
var otel = services.AddOpenTelemetry()
    .ConfigureResource(rb =>
    {
        _ = rb.AddService(
                environment.ApplicationName,
                serviceNamespace: "DrEsteban",
                serviceVersion: Assembly.GetExecutingAssembly().GetName().Version?.ToString())
            .AddAttributes([KeyValuePair.Create<string, object>("ASPNETCORE_ENVIRONMENT", environment.EnvironmentName)])
            .AddContainerDetector()
            .AddEnvironmentVariableDetector()
            .AddHostDetector()
            .AddOperatingSystemDetector()
            .AddProcessDetector()
            .AddProcessRuntimeDetector()
            .AddTelemetrySdk();
    })
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
            .AddRuntimeInstrumentation()
            .AddPrometheusExporter();
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
services.Configure<PrometheusAspNetCoreOptions>(options =>
{
    options.ScrapeResponseCacheDurationMilliseconds = (int)TimeSpan.FromSeconds(responseCacheConfiguration.ResponseCacheDurationSeconds).TotalMilliseconds;
});

// Prometheus
// TODO: Consider refactoring to use OpenTelemetry Prometheus exporter instead of Prometheus client.
//       Would require re-writing solar metrics as observable .NET Meters.
services.AddSingleton<CollectorRegistry>(Metrics.NewCustomRegistry());

// Http
services.AddSingleton<IOptions<ResponseCacheConfiguration>>(Options.Create(responseCacheConfiguration));
services.AddMemoryCache();
services.AddHttpContextAccessor();
services.AddHealthChecks()
    .AddCheck<MetricsHealthCheck>("Metrics");
services.AddTransient<OutboundHttpClientLogger>();

// Collectors:
// Tesla
if (configuration.GetValue<bool>("Tesla:Enabled"))
{
    services.AddOptionsWithValidateOnStart<TeslaConfiguration>()
        .BindConfiguration("Tesla")
        .ValidateDataAnnotations();

    services.AddHttpClient<TeslaGatewayMetricsService>((c, client) =>
        {
            var teslaConfig = c.GetRequiredService<IOptions<TeslaConfiguration>>().Value;
            client.BaseAddress = new Uri($"https://{teslaConfig.Host}");
            // The Tesla Gateway only accepts a certain set of Host header values
            client.DefaultRequestHeaders.Host = teslaConfig.RequestHost;
            client.Timeout = TimeSpan.FromSeconds(teslaConfig.RequestTimeoutSeconds);
        })
        .ConfigurePrimaryHttpMessageHandler(_ =>
        {
            var handler = new HttpClientHandler
            {
                // Tesla Gateway serves a self-signed cert
                ServerCertificateCustomValidationCallback = (_, _, _, _) => true
            };
            return handler;
        })
        .UseHttpClientMetrics()
        .RemoveAllLoggers()
        .AddLogger<OutboundHttpClientLogger>();
    services.AddSingleton<IMetricsService, TeslaGatewayMetricsService>();
}

// Enphase
if (configuration.GetValue<bool>("Enphase:Enabled"))
{
    services.AddOptionsWithValidateOnStart<EnphaseConfiguration>()
        .BindConfiguration("Enphase")
        .ValidateDataAnnotations();
    services.AddHttpClient<IMetricsService, EnphaseMetricsService>((c, client) =>
        {
            var enphaseConfig = c.GetRequiredService<IOptions<EnphaseConfiguration>>().Value;
            client.BaseAddress = new Uri($"http://{enphaseConfig.Host}");
            client.Timeout = TimeSpan.FromSeconds(enphaseConfig.RequestTimeoutSeconds);
        })
        .UseHttpClientMetrics()
        .RemoveAllLoggers()
        .AddLogger<OutboundHttpClientLogger>();
    services.AddSingleton<IMetricsService, EnphaseMetricsService>();
}

services.AddSingleton<IMetricsCollectionService, MetricsCollectionService>();

// -----------------
// Build app:
await using var app = builder.Build();

// Configure the HTTP request pipeline:
//
// Since we have no auth, go ahead and always use developer exception page.
app.UseDeveloperExceptionPage();
// This endpoint provides Prometheus metrics about the app itself.
// It is not the same as the /metrics endpoint that proxies metrics from other services.
app.UseOpenTelemetryPrometheusScrapingEndpoint("/appmetrics");
if (app.Configuration.GetValue<bool>("EnableConfigEndpoints", false))
{
    app.MapGet("/config/tesla", (IOptions<TeslaConfiguration> config) => Results.Ok(config.Value));
    app.MapGet("/config/enphase", (IOptions<EnphaseConfiguration> config) => Results.Ok(config.Value));
}
app.MapMetricsEndpoints();
app.MapHealthChecks("/health");

// Initialize logins
try
{
    var metricsServices = app.Services.GetServices<IMetricsService>();
    foreach (var service in metricsServices)
    {
        await service.EnsureLoggedInAsync();
    }
}
catch (Exception ex)
{
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    logger.LogWarning(ex, "Failed to initialize metrics services login.");
}

// Run the app:
await app.RunAsync();
