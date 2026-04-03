namespace Lab1.Infrastructure.Persistence.Model.PayloadModel;

public record PayInvoicePayload(
    long InvoiceId,
    decimal Amount,
    long RecipientId) : Payload;