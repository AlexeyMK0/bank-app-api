using BankApp.Gateway.Application.Models;

namespace BankApp.Gateway.Presentation.Http.Operations.Accounts.Responses;

public record CreateAccountResponse(AccountDto CreatedAccount);