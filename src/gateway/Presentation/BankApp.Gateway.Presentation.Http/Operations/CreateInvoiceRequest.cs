namespace BankApp.Gateway.Presentation.Http.Operations;

public record CreateInvoiceRequest(
    Guid SessionId,
    long PayerId,
    decimal Amount);