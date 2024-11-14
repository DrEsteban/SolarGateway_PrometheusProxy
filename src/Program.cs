using System.Reflection;
using Azure.Monitor.OpenTelemetry.Exporter;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
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
string? appInsightsConnectionString = builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"];
bool useOtlpExporter = !string.IsNullOrWhiteSpace(builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]);
services.AddOpenTelemetry()
    .ConfigureResource(rb =>
    {
        rb.AddService(
            builder.Environment.ApplicationName,
            serviceNamespace: "DrEsteban",
            serviceVersion: Assembly.GetExecutingAssembly().GetName().Version?.ToString())
            .AddAttributes([KeyValuePair.Create<string, object>("ASPNETCORE_ENVIRONMENT", builder.Environment.EnvironmentName)])
            .AddContainerDetector()
            .AddEnvironmentVariableDetector()
            .AddHostDetector()
            .AddProcessDetector()
            .AddProcessRuntimeDetector()
            .AddTelemetrySdk();
    })
    .WithLogging(o =>
    {
        if (!string.IsNullOrWhiteSpace(appInsightsConnectionString))
        {
            o.AddAzureMonitorLogExporter(o => o.ConnectionString = appInsightsConnectionString);
        }
        if (useOtlpExporter)
        {
            o.AddOtlpExporter();
        }
    })
    .WithTracing(o =>
    {
        o.AddProcessor<MyHttpTraceActivityProcessor>()
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .SetErrorStatusOnException();
        if (!string.IsNullOrWhiteSpace(appInsightsConnectionString))
        {
            o.AddAzureMonitorTraceExporter(o => o.ConnectionString = appInsightsConnectionString);
        }
        if (useOtlpExporter)
        {
            o.AddOtlpExporter();
        }
    })
    .WithMetrics(o =>
    {
        o.AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddProcessInstrumentation()
            .AddRuntimeInstrumentation();
        if (!string.IsNullOrWhiteSpace(appInsightsConnectionString))
        {
            o.AddAzureMonitorMetricExporter(o => o.ConnectionString = appInsightsConnectionString);
        }
        if (useOtlpExporter)
        {
            o.AddOtlpExporter();
        }
    });
services.AddMetrics();

// Prometheus
// TODO: Consider refactoring to use OpenTelemetry Prometheus exporter instead of Prometheus client.
//       Would require re-writing solar metrics as observable .NET Meters.
services.AddSingleton<CollectorRegistry>(Metrics.DefaultRegistry);

// Http
var responseCacheConfiguration = builder.Configuration.Get<ResponseCacheConfiguration>() ?? new();
services.AddSingleton<IOptions<ResponseCacheConfiguration>>(new OptionsWrapper<ResponseCacheConfiguration>(responseCacheConfiguration));
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
var app = builder.Build();

// Configure the HTTP request pipeline.
// Since we have no auth, go ahead and always use developer exception page.
app.UseDeveloperExceptionPage();
app.UseResponseCaching();
app.MapControllers();

app.Run();