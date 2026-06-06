namespace BankApp.Gateway.Application.Abstractions.Requests.Approval;

public static class AssignAccountant
{
    public sealed record Request(long InvoiceId, long AccountantId);
}