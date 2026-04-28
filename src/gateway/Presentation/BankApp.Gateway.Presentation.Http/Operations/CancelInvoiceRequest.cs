namespace BankApp.Gateway.Presentation.Http.Operations;

public sealed record CancelInvoiceRequest(
    long InvoiceId);