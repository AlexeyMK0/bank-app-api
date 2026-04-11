using BankApp.Domain.Accounts;
using BankApp.Domain.Invoices;
using BankApp.Domain.ValueObjects;

namespace BankApp.Domain.Operations.Implementation;

public record PaymentReceivedOperationRecord(
    OperationRecordId Id,
    DateTimeOffset Time,
    AccountId AccountId,
    InvoiceId InvoiceId,
    Money Amount)
    : OperationRecord(Id, Time, AccountId);