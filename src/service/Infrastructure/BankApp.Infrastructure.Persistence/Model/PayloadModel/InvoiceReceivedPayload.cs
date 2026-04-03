namespace Lab1.Infrastructure.Persistence.Model.PayloadModel;

public record InvoiceReceivedPayload(
    long InvoiceId,
    decimal Amount,
    long RecipientId) : Payload;