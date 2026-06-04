namespace BankApp.Gateway.Application.Abstractions.Requests.Approval;

public static class DeclineInvoice
{
    public sealed record Request(long InvoiceId, long UserId);
}