namespace BankApp.Gateway.Application.Abstractions.Requests;

public static class CreateInvoice
{
    public sealed record Response(long InvoiceId);
}