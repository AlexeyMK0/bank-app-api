namespace BankApp.Gateway.Presentation.Http.Operations;

public sealed record AssignAccountantRequest(long InvoiceId, long UserId);