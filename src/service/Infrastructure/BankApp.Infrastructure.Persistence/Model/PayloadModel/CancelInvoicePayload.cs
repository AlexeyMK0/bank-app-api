namespace Lab1.Infrastructure.Persistence.Model.PayloadModel;

// TODO: maybe add custom converter
public record CancelInvoicePayload
    (
        long InvoiceId,
        decimal Amount,
        long RecipientId,
        long PayerId) : Payload;