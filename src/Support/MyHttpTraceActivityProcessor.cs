using System.Diagnostics;
using System.Net;
using OpenTelemetry;

namespace SolarGateway_PrometheusProxy.Support;

public class MyHttpTraceActivityProcessor(IHttpContextAccessor _httpContextAccessor) : BaseProcessor<Activity>
{
    public override void OnEnd(Activity activity)
    {
        var context = _httpContextAccessor?.HttpContext;
        if (context == null)
        {
            return;
        }

        // Set Request and Response Headers
        if (context.Request?.Headers != null)
        {
            foreach (var header in context.Request.Headers)
            {
                string key = $"Request-{header.Key}";
                if (header.Value.Count != 0)
                {
                    activity.SetTag(key, string.Join(", ", header.Value.Where(h => h != null)));
                }
                else
                {
                    activity.SetTag(key, string.Empty);
                }
            }
        }
        if (context.Response?.Headers != null)
        {
            foreach (var header in context.Response.Headers)
            {
                string key = $"Response-{header.Key}";
                if (header.Value.Count != 0)
                {
                    activity.SetTag(key, string.Join(", ", header.Value.Where(h => h != null)));
                }
                else
                {
                    activity.SetTag(key, string.Empty);
                }
            }
        }

        // Fix Forwarded Headers...
        const string ClientIP = "client.address";
        const string Scheme = "url.scheme";
        const string Host = "server.address";
        const string Port = "server.port";
        const string Path = "url.path";
        var request = context.Request;
        if (request != null)
        {
            var connection = context.Connection;
            var path = (request.PathBase.HasValue || request.Path.HasValue) ? (request.PathBase + request.Path).ToString() : "/";
            SetIpAddressTagIfDifferent(activity, ClientIP, connection.RemoteIpAddress?.ToString());
            SetStringTagIfDifferent(activity, Scheme, request.Scheme);
            SetStringTagIfDifferent(activity, Host, request.Host.Host);
            SetIntTagIfDifferent(activity, Port, request.Host.Port);
            SetStringTagIfDifferent(activity, Path, path);
        }
    }

    private static void SetIpAddressTagIfDifferent(Activity activity, string key, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        object? currentTag = activity.GetTagItem(key);
        if (currentTag == null) // If null, set
        {
            activity.SetTag(key, value);
        }
        else if (currentTag is string currentStr)
        {
            if (currentStr != value) // Only set if different
            {
                activity.SetTag(key, value);
            }
        }
        else if (currentTag is IPAddress currentAddr)
        {
            if (currentAddr.ToString() != value) // Only set if different
            {
                activity.SetTag(key, value);
            }
        }
        else // Unrecognized existing type
        {
            activity.SetTag(key + ".unrecognized_value", value);
            activity.SetTag(key + ".unrecognized_type", currentTag.GetType().FullName);
        }
    }

    private static void SetStringTagIfDifferent(Activity activity, string key, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        object? currentTag = activity.GetTagItem(key);
        if (currentTag == null) // If null, set
        {
            activity.SetTag(key, value);
        }
        else if (currentTag is string currentStr)
        {
            if (currentStr != value) // Only set if different
            {
                activity.SetTag(key, value);
            }
        }
        else // Unrecognized existing type
        {
            activity.SetTag(key + ".unrecognized_value", value);
            activity.SetTag(key + ".unrecognized_type", currentTag.GetType().FullName);
        }
    }

    private static void SetIntTagIfDifferent(Activity activity, string key, int? valueNullable)
    {
        if (!valueNullable.HasValue)
        {
            return;
        }
        int value = valueNullable.Value;

        object? currentTag = activity.GetTagItem(key);
        if (currentTag == null) // If null, set
        {
            activity.SetTag(key, value);
        }
        else if (currentTag is int currentInt)
        {
            if (currentInt != value) // Only set if different
            {
                activity.SetTag(key, value);
            }
        }
        else if (currentTag is string currentString)
        {
            if (currentString != value.ToString()) // Only set if different
            {
                activity.SetTag(key, value);
            }
        }
        else // Unrecognized existing type
        {
            activity.SetTag(key + ".unrecognized_value", value);
            activity.SetTag(key + ".unrecognized_type", currentTag.GetType().FullName);
        }
    }
}
