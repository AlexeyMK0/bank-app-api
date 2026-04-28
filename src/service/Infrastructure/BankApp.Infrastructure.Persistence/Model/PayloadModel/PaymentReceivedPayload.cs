namespace BankApp.Infrastructure.Persistence.Model.PayloadModel;

internal sealed record PaymentReceivedPayload(
    long InvoiceId,
    decimal Amount) : Payload;