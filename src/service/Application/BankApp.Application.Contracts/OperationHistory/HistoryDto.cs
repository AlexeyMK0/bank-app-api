namespace Contracts.OperationHistory;

public record HistoryDto(IReadOnlyCollection<OperationDto> Operations);