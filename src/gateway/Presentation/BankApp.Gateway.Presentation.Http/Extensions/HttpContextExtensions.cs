using System.Security.Claims;

namespace BankApp.Gateway.Presentation.Http.Extensions;

public static class HttpContextExtensions
{
    public static Guid GetCurrentUserId(this HttpContext context)
    {
        string userIdStr = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? throw new ArgumentException("Bad JWT token: userId not found");
        return Guid.Parse(userIdStr);
    }
}