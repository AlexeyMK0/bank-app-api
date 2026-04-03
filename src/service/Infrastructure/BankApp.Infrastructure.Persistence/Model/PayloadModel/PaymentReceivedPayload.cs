namespace Lab1.Infrastructure.Persistence.Model.PayloadModel;

public record PaymentReceivedPayload(
    long InvoiceId,
    decimal Amount,
    long PayerId) : Payload;