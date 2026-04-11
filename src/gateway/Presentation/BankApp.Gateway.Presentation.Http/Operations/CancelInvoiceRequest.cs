namespace BankApp.Gateway.Presentation.Http.Operations;

public sealed record CancelInvoiceRequest(
    Guid SessionId,
    long InvoiceId);