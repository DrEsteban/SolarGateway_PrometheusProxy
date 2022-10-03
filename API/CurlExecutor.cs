using System.Diagnostics;
using System.Text.Json;

namespace TeslaGateway_PrometheusProxy;

/// <summary>
/// Unfortunately we have to use `curl` to fetch data from the Tesla Gateway. The gateway
/// uses a self-signed certificate (and requires access over HTTPS), and .NET appears to have
/// a bug on Linux where you cannot configure HttpClient to ignore certificate validation errors.
/// </summary>
public static class CurlExecutor
{
    public static Task<(bool success, string output)> ExecuteCurlAsync(string url, string authToken = null)
    {
        return ExecuteCurlInnerAsync(url, null, authToken);
    }

    public static Task<(bool success, string output)> ExecuteCurlAsync<T>(string url, T body, string authToken = null)
    {
        string bodyStr = JsonSerializer.SerializeToElement(body).ToString();
        return ExecuteCurlInnerAsync(url, bodyStr, authToken);
    }

    private static async Task<(bool success, string output)> ExecuteCurlInnerAsync(string url, string? body, string? authToken)
    {
        var pse = new ProcessStartInfo("curl")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        pse.ArgumentList.Add(url);
        pse.ArgumentList.Add("--insecure");
        pse.ArgumentList.Add("-f");

        if (!string.IsNullOrEmpty(body))
        {
            pse.ArgumentList.Add("-d");
            pse.ArgumentList.Add(body);
            pse.ArgumentList.Add("-H");
            pse.ArgumentList.Add("Content-Type: application/json");
        }
        if (!string.IsNullOrEmpty(authToken))
        {
            pse.ArgumentList.Add("-H");
            pse.ArgumentList.Add($"Authorization: Bearer {authToken}");
        }

        using var process = Process.Start(pse)!;
        await process.WaitForExitAsync();

        string output = await process.StandardOutput.ReadToEndAsync();
        return (process.ExitCode == 0, output);
    }
}