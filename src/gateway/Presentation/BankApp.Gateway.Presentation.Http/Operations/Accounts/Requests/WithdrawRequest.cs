using System.ComponentModel.DataAnnotations;

namespace BankApp.Gateway.Presentation.Http.Operations.Accounts.Requests;

public sealed class WithdrawRequest
{
    [Range(0.01, double.MaxValue)]
    public required decimal Amount { get; init; }
}