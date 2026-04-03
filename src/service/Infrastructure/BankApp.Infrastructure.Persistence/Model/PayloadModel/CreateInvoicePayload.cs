namespace Lab1.Infrastructure.Persistence.Model.PayloadModel;

public record CreateInvoicePayload(
    long InvoiceId,
    decimal Amount,
    long PayerId) : Payload;