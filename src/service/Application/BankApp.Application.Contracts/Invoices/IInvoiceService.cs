using Contracts.Invoices.Operations;

namespace Contracts.Invoices;

public interface IInvoiceService
{
    Task<CreateInvoice.Response> CreateInvoiceAsync(
        CreateInvoice.Request request,
        CancellationToken cancellationToken);

    Task<CancelInvoice.Response> CancelInvoiceAsync(
        CancelInvoice.Request request,
        CancellationToken cancellationToken);

    Task<PayInvoice.Response> PayInvoiceAsync(
        PayInvoice.Request request,
        CancellationToken cancellationToken);

    Task<GetIncomingInvoices.Response> GetIncomingInvoicesAsync(
        GetIncomingInvoices.Request request,
        CancellationToken cancellationToken);

    Task<GetOutgoingInvoices.Response> GetOutgoingInvoicesAsync(
        GetOutgoingInvoices.Request request,
        CancellationToken cancellationToken);
}