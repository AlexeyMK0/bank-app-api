namespace BankApp.Gateway.Application.Models.Operations;

public sealed record PayInvoiceOperationDto(
    long Id,
    DateTimeOffset Time,
    long AccountId,
    long InvoiceId,
    decimal Amount) : OperationRecordDto(Id, Time, AccountId);