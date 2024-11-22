using System.Reflection;
using Azure.Monitor.OpenTelemetry.AspNetCore;
using Microsoft.AspNetCore.Mvc;
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
using SolarGateway_PrometheusProxy.Models;
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
    .WithLogging()
    .WithTracing(o =>
    {
        o.AddSource("Polly")
            .AddProcessor<MyHttpTraceActivityProcessor>()
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .SetErrorStatusOnException();
    })
    .WithMetrics(o =>
    {
        o.AddMeter("Polly")
            .AddAspNetCoreInstrumentation()
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
if (configuration.GetValue<bool>("TeslaGateway:Enabled"))
{
    services.Configure<TeslaLoginRequest>(configuration.GetSection("TeslaGateway"));
    services.Configure<TeslaConfiguration>(configuration.GetSection("TeslaGateway"));
    services.AddHttpClient<IMetricsService, TeslaGatewayMetricsService>(client =>
        {
            client.BaseAddress = new Uri($"https://{configuration["TeslaGateway:Host"]}");
            // The Tesla Gateway only accepts a certain set of Host header values
            string? requestHostOverride = configuration["TeslaGateway:RequestHost"];
            client.DefaultRequestHeaders.Host = string.IsNullOrWhiteSpace(requestHostOverride) ? "powerwall" : requestHostOverride;
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
}

// Enphase
if (configuration.GetValue<bool>("Enphase:Enabled"))
{
    services.AddHttpClient<IMetricsService, EnphaseMetricsService>(client =>
        {
            client.BaseAddress = new Uri($"http://{configuration["Enphase:Host"]}");
        })
        .UseHttpClientMetrics()
        .RemoveAllLoggers()
        .AddLogger<OutboundHttpClientLogger>();
}

// -----------------
// Build app:
await using var app = builder.Build();

// Configure the HTTP request pipeline:
//
// Since we have no auth, go ahead and always use developer exception page.
app.UseDeveloperExceptionPage();
app.UseResponseCaching();
// This endpoint provides Prometheus metrics about the app itself.
// It is not the same as the Controller-based /metrics endpoint that proxies metrics from other services.
app.UseOpenTelemetryPrometheusScrapingEndpoint("/appmetrics");
app.MapControllers();

// Run the app:
await app.RunAsync();