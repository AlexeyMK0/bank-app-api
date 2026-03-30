using Contracts.Accounts.Model;

namespace Contracts.OperationHistory;

public record HistoryDto(IReadOnlyCollection<OperationRecordDto> Operations);