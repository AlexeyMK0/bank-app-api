using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace BankApp.ServiceDefaults.Extensions;

public static class ActivityExtensions
{
    public static bool TryGetTag(this Activity activity, string key, [NotNullWhen(true)] out string? tag)
    {
        tag = activity.GetTagItem(key) as string;
        return tag is not null;
    }

    public static void Suppress(this Activity activity)
    {
        // Ignore this activity for exporters
        activity.ActivityTraceFlags &= ~ActivityTraceFlags.Recorded;
    }
}