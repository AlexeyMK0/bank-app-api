using BankApp.Gateway.Application.Models;

namespace BankApp.Gateway.Presentation.Http.Operations.OperationHistory;

public sealed record GetOperationHistoryResponse(
    IEnumerable<OperationRecordDto> Operations,
    string? PageToken);