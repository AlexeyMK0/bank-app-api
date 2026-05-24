using BankApp.ServiceDefaults.Extensions;
using OpenTelemetry;
using System.Diagnostics;

namespace BankApp.ServiceDefaults.Processors;

public class OpenTelemetryTraceSuppressor : BaseProcessor<Activity>
{
    public override void OnEnd(Activity data)
    {
        if (data.TryGetTag("rpc.service", out string? service)
            && service.Contains("opentelemetry", StringComparison.OrdinalIgnoreCase))
        {
            data.Suppress();
        }
    }
}