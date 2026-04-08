using Lab1.Domain.Accounts;
using Lab1.Domain.Invoices;
using Lab1.Domain.ValueObjects;

namespace Lab1.Domain.Operations.Implementation;

public record PaymentReceivedOperationRecord(
    OperationRecordId Id,
    DateTimeOffset Time,
    AccountId AccountId,
    InvoiceId InvoiceId,
    Money Amount)
    : OperationRecord(Id, Time, AccountId);