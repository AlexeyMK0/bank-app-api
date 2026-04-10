namespace BankApp.Gateway.Application.Models.Operations;

public record PaymentReceivedOperationDto(
    long Id,
    DateTimeOffset Time,
    long AccountId,
    long InvoiceId,
    decimal Amount) : OperationRecordDto(Id, Time, AccountId);