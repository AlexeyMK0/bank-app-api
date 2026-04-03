namespace Contracts.OperationHistory.Operations;

public record InvoiceReceivedOperationDto(
    long Id,
    DateTimeOffset Time,
    long AccountId,
    Guid SessionId,
    long InvoiceId,
    decimal Amount,
    long RecipientId) : OperationDto(Id, Time, AccountId, SessionId);