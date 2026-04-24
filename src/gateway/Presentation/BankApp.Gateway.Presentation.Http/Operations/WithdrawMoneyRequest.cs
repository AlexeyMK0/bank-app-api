using System.ComponentModel.DataAnnotations;

namespace BankApp.Gateway.Presentation.Http.Operations;

public sealed class WithdrawMoneyRequest
{
    [Range(0.01, double.MaxValue)]
    public required decimal Amount { get; init; }

    public required long AccountId { get; init; }
}