namespace BankApp.Gateway.Application.Models.Responses;

public sealed record GetOperationHistoryResponse(
    IEnumerable<OperationRecordDto> Operations,
    string? PageToken);