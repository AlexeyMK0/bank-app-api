using BankApp.Gateway.Application.Models;

namespace BankApp.Gateway.Presentation.Http.Responses;

public sealed record GetUserAccountsResponse(IEnumerable<AccountDto> Accounts, string? PageToken);