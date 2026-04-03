namespace Contracts.OperationHistory.Operations;

public record PayInvoiceOperationDto(
    long Id,
    DateTimeOffset Time,
    long AccountId,
    Guid SessionId,
    long InvoiceId,
    decimal Amount,
    long RecipientId) : OperationDto(Id, Time, AccountId, SessionId);