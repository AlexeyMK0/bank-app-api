namespace BankApp.Gateway.Application.Models.Responses;

public record GetOperationHistoryResponseDto(
    IEnumerable<OperationRecordDto> Operations,
    string? PageToken);