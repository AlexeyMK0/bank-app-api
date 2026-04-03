namespace Contracts.OperationHistory.Operations;

public record InvoiceWasCancelledOperationDto(
    long Id,
    DateTimeOffset Time,
    long AccountId,
    Guid SessionId,
    long InvoiceId,
    decimal Amount,
    long RecipientId,
    long PayerId) : OperationDto(Id, Time, AccountId, SessionId);