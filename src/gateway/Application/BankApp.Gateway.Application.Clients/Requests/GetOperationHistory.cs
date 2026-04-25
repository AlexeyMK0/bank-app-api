using BankApp.Gateway.Application.Models;

namespace BankApp.Gateway.Application.Abstractions.Requests;

public static class GetOperationHistory
{
    public sealed record Response(
        IEnumerable<OperationRecordDto> Operations,
        string? PageToken);
}