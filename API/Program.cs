using Microsoft.Extensions.Options;
using Prometheus;
using TeslaGateway_PrometheusProxy;
using TeslaGateway_PrometheusProxy.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddMemoryCache();
builder.Services.Configure<LoginRequest>(builder.Configuration.GetSection("TeslaGateway"));
builder.Services.Configure<TeslaGatewaySettings>(builder.Configuration.GetSection("TeslaGateway"));
builder.Services.AddSingleton<CollectorRegistry>(c =>
{
    var registry = Metrics.NewCustomRegistry();
    registry.SetStaticLabels(new Dictionary<string, string>()
    {
        { "Host", c.GetRequiredService<IOptions<TeslaGatewaySettings>>().Value.Host }
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
