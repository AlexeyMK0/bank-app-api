using OpenTelemetry;
using System.Diagnostics;

namespace BankApp.ServiceDefaults.Processors;

public class BaggageToTagsProcessor : BaseProcessor<Activity>
{
    public override void OnStart(Activity data)
    {
        foreach (KeyValuePair<string, string?> pair in data.Baggage)
        {
            data.AddTag(pair.Key, pair.Value);
        }
    }
}