namespace BankApp.Gateway.Presentation.Http.Operations;

public record CancelInvoiceRequest(
    Guid SessionId,
    long InvoiceId);