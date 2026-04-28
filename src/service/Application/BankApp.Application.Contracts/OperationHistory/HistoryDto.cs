namespace BankApp.Application.Contracts.OperationHistory;

public sealed record HistoryDto(IReadOnlyCollection<OperationDto> Operations);