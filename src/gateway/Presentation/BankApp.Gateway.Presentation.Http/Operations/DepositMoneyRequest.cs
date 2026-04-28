using System.ComponentModel.DataAnnotations;

namespace BankApp.Gateway.Presentation.Http.Operations;

public sealed class DepositMoneyRequest
{
    [Range(0.01, double.MaxValue)]
    public required decimal Amount { get; init; }

    public required Guid SessionId { get; init; }
}