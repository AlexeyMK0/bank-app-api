namespace Contracts.OperationHistory.Operations;

public record PayInvoiceOperationDto(
    long Id,
    DateTimeOffset Time,
    long AccountId,
    long InvoiceId,
    decimal Amount) : OperationDto(Id, Time, AccountId);