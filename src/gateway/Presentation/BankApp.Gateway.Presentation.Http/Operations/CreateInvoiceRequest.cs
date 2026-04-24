namespace BankApp.Gateway.Presentation.Http.Operations;

public sealed record CreateInvoiceRequest(
    long PayerId,
    decimal Amount);