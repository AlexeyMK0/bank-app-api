using BankApp.Gateway.Application.Abstractions.Requests;

namespace BankApp.Gateway.Application.Abstractions.Clients;

public interface IInvoiceClient
{
    Task<CreateInvoice.Response> CreateInvoiceAsync(Guid userId, long payerId, long recipientId, decimal amount, CancellationToken cancellationToken);

    Task PayInvoiceAsync(Guid userId, long invoiceId, CancellationToken cancellationToken);

    Task CancelInvoiceAsync(Guid userId, long invoiceId, CancellationToken cancellationToken);

    Task<GetIncomingInvoices.Response> GetIncomingInvoicesAsync(
        GetIncomingInvoices.Request request,
        CancellationToken cancellationToken);

    Task<GetOutgoingInvoices.Response> GetOutgoingInvoicesAsync(
        GetOutgoingInvoices.Request request,
        CancellationToken cancellationToken);
}