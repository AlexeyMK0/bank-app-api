namespace Lab1.Infrastructure.Persistence.Model.PayloadModel;

public record InvoiceWasCancelledPayload(
    long InvoiceId,
    decimal Amount,
    long RecipientId,
    long PayerId) : Payload;