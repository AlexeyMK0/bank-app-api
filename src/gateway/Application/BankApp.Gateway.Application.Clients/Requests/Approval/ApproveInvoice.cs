namespace BankApp.Gateway.Application.Abstractions.Requests.Approval;

public static class ApproveInvoice
{
    public sealed record Request(long InvoiceId, long UserId);
}