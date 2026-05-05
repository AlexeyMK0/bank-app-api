using Microsoft.Extensions.Logging;

namespace BankApp.Application.Extensions.LoggerExtensions;

public static partial class CommonLoggerExtensions
{
    [LoggerMessage(
        LogLevel.Warning,
        "User with external id {ExternalId} not found")]
    public static partial void LogUserWithExternalIdNotFound(this ILogger logger, Guid externalId);

    [LoggerMessage(
        LogLevel.Information,
        "User {UserId} attempted to find non-existing account {accountId}")]
    public static partial void LogAccountNotFound(this ILogger logger, long userId, long accountId);

    [LoggerMessage(
        LogLevel.Warning,
        "User {UserId} attempted to access account {accountId} owned by {AccountOwnerId} in operation {OperationName}")]
    public static partial void LogUnauthorizedAccess(
        this ILogger logger,
        long userId,
        long accountId,
        long accountOwnerId,
        string operationName);

    [LoggerMessage(
        LogLevel.Warning,
        "User {UserId} attempted to access accounts they do not own. Requested: {RequestCount}, Unauthorized: {UnauthorizedCount}. UnauthorizedIds: {AccountIds}")]
    public static partial void LogUnauthorizedAccountBatchAccess(this ILogger logger, long userId, long requestCount, long unauthorizedCount, string accountIds);

    [LoggerMessage(
        LogLevel.Information,
        "User {UserId} successfully completed operation {OperationName}")]
    public static partial void LogUserCompletedOperation(this ILogger logger, long userId, string operationName);
}