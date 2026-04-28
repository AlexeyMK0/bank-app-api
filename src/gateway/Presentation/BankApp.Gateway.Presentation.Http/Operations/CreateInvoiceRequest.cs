namespace BankApp.Gateway.Presentation.Http.Operations;

public sealed record CreateInvoiceRequest(
    Guid SessionId,
    long PayerId,
    decimal Amount);