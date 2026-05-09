using BankApp.Gateway.Application.Abstractions.Clients;
using BankApp.Gateway.Application.Abstractions.Requests;
using BankApp.Gateway.Infrastructure.Service.Mappers;
using BankApp.Grpc;
using Google.Type;

namespace BankApp.Gateway.Infrastructure.Service.Clients;

public class InvoiceClient : IInvoiceClient
{
    private readonly InvoiceService.InvoiceServiceClient _client;

    public InvoiceClient(InvoiceService.InvoiceServiceClient client)
    {
        _client = client;
    }

    public async Task<CreateInvoice.Response> CreateInvoiceAsync(
        Guid userId,
        long payerId,
        long recipientId,
        decimal amount,
        CancellationToken cancellationToken)
    {
        var money = new Money { DecimalValue = amount };

        var request = new ProtoCreateInvoiceRequest(userId.ToString(), payerId, recipientId, money);
        ProtoCreateInvoiceResponse response =
            await _client.CreateInvoiceAsync(request, cancellationToken: cancellationToken);
        return new CreateInvoice.Response(response.InvoiceId);
    }

    public async Task PayInvoiceAsync(Guid userId, long invoiceId, CancellationToken cancellationToken)
    {
        var request = new ProtoPayInvoiceRequest(userId.ToString(), invoiceId);
        await _client.PayInvoiceAsync(request, cancellationToken: cancellationToken);
    }

    public async Task CancelInvoiceAsync(Guid userId, long invoiceId, CancellationToken cancellationToken)
    {
        var request = new ProtoCancelInvoiceRequest(userId.ToString(), invoiceId);
        await _client.CancelInvoiceAsync(request, cancellationToken: cancellationToken);
    }

    public async Task<GetInvoices.Response> GetInvoicesAsync(GetInvoices.Request request, CancellationToken cancellationToken)
    {
        IEnumerable<ProtoInvoiceStatus> states = request.Statuses.Select(status => status.MapToProto());
        var protoRequest = new ProtoGetInvoicesRequest(
            request.UserId.ToString(),
            request.PageToken,
            request.PageSize,
            states,
            request.PayerIds,
            request.RecipientIds);
        ProtoGetInvoicesResponse response = await _client
            .GetInvoicesAsync(protoRequest, cancellationToken: cancellationToken);
        return new GetInvoices.Response(
            response.Invoices.Select(invoice => invoice
                .MapToDto()),
            response.PageToken);
    }
}