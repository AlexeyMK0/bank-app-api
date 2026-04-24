using BankApp.Gateway.Application.Models;

namespace BankApp.Gateway.Application.Abstractions.Requests;

public sealed class GetOperationHistory
{
    public sealed record Response(
        IEnumerable<OperationRecordDto> Operations,
        string? PageToken);
}