namespace BankApp.Infrastructure.Persistence.Model.PayloadModel;

internal sealed record PayInvoicePayload(
    long InvoiceId,
    decimal Amount) : Payload;