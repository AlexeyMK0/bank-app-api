namespace BankApp.Gateway.Presentation.Http.Operations.Invoices.Requests;

public sealed record CreateInvoiceRequest(
    long PayerId,
    decimal Amount,
    long RecipientId);