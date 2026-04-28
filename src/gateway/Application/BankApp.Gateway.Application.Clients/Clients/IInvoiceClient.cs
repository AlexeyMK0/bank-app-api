using BankApp.Gateway.Application.Abstractions.Requests;
using BankApp.Gateway.Application.Models.Responses;

namespace BankApp.Gateway.Application.Abstractions.Clients;

public interface IInvoiceClient
{
    Task<CreateInvoice.Response> CreateInvoiceAsync(Guid sessionId, long payerId, decimal amount, CancellationToken cancellationToken);

    Task PayInvoiceAsync(Guid sessionId, long invoiceId, CancellationToken cancellationToken);

    Task CancelInvoiceAsync(Guid sessionId, long invoiceId, CancellationToken cancellationToken);

    Task<GetIncomingInvoicesResponse> GetIncomingInvoicesAsync(
        GetIncomingInvoices.Request request,
        CancellationToken cancellationToken);

    Task<GetOutgoingInvoicesResponse> GetOutgoingInvoicesAsync(
        GetOutgoingInvoices.Request request,
        CancellationToken cancellationToken);
}