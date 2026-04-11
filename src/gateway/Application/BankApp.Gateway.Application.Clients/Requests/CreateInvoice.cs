namespace BankApp.Gateway.Application.Abstractions.Requests;

public sealed class CreateInvoice
{
    public sealed record Response(long InvoiceId);
}