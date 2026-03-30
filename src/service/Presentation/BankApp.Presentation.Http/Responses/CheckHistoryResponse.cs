using Contracts.Accounts.Model;

namespace Lab1.Presentation.Http.Responses;

public record CheckHistoryResponse(
    IReadOnlyCollection<OperationRecordDto> Operations,
    string? PageToken);