using BankApp.Gateway.Application.Models;

namespace BankApp.Gateway.Application.Abstractions.Requests;

public static class GetUserAccounts
{
    public sealed record Request(Guid UserId, int PageSize, string? PageToken = null);

    public sealed record Response(IEnumerable<AccountDto> Accounts, string? PageToken = null);
}