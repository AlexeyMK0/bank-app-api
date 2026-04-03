namespace Contracts.OperationHistory.Operations;

public record CreateInvoiceOperationDto(
    long Id,
    DateTimeOffset Time,
    long AccountId,
    Guid SessionId,
    long InvoiceId,
    decimal Amount,
    long PayerId) : OperationDto(Id, Time, AccountId, SessionId);