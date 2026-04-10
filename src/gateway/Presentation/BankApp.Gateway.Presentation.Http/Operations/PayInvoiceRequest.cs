namespace BankApp.Gateway.Presentation.Http.Operations;

public record PayInvoiceRequest(Guid SessionId, long InoviceId);