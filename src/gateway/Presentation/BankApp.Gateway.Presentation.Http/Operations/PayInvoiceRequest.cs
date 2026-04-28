namespace BankApp.Gateway.Presentation.Http.Operations;

public sealed record PayInvoiceRequest(Guid SessionId, long InoviceId);