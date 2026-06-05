using BankApp.Gateway.Application.Models;

namespace BankApp.Gateway.Presentation.Http.Operations.Accounts.Responses;

public sealed record GetUserAccountsResponse(IEnumerable<AccountDto> Accounts, string? PageToken);