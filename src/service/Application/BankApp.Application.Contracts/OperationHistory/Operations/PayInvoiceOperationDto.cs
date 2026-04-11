namespace BankApp.Application.Contracts.OperationHistory.Operations;

public sealed record PayInvoiceOperationDto(
    long Id,
    DateTimeOffset Time,
    long AccountId,
    long InvoiceId,
    decimal Amount) : OperationDto(Id, Time, AccountId);