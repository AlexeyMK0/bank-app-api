using BankApp.Gateway.Application.Models;

namespace BankApp.Gateway.Presentation.Http.Responses;

public sealed record GetOperationHistoryResponse(
    IEnumerable<OperationRecordDto> Operations,
    string? PageToken);