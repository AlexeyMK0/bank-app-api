using System.Diagnostics;

namespace BankApp.Gateway.Presentation.Http.Extensions;

public static class ActivityExtension
{
    public static void AddUserIdBaggage(this Activity activity, Guid userId)
    {
        activity.AddTag("user.id", userId.ToString());
        activity.AddBaggage("user.id", userId.ToString());
    }

    public static void AddAccountIdBaggage(this Activity activity, long accountId)
    {
        activity.AddTag("account.id", accountId.ToString());
        activity.AddBaggage("account.id", accountId.ToString());
    }
}