using BankApp.Gateway.Application.Abstractions.Clients;
using BankApp.Gateway.Application.Abstractions.Requests;
using BankApp.Gateway.Infrastructure.Service.Mappers;
using BankApp.Grpc;
using Google.Type;
using GetIncomingInvoicesResponse = BankApp.Gateway.Application.Models.Responses.GetIncomingInvoicesResponse;
using GetOutgoingInvoicesResponse = BankApp.Gateway.Application.Models.Responses.GetOutgoingInvoicesResponse;

namespace BankApp.Gateway.Infrastructure.Service.Clients;

public class InvoiceClient : IInvoiceClient
{
    private readonly InvoiceService.InvoiceServiceClient _client;

    public InvoiceClient(InvoiceService.InvoiceServiceClient client)
    {
        _client = client;
    }

    public async Task<CreateInvoice.Response> CreateInvoiceAsync(
        Guid sessionId,
        long payerId,
        decimal amount,
        CancellationToken cancellationToken)
    {
        var request = new CreateInvoiceRequest(sessionId.ToString(), payerId, new Money { DecimalValue = amount });
        CreateInvoiceResponse response =
            await _client.CreateInvoiceAsync(request, cancellationToken: cancellationToken);
        return new CreateInvoice.Response(response.InvoiceId);
    }

    public async Task PayInvoiceAsync(Guid sessionId, long invoiceId, CancellationToken cancellationToken)
    {
        var request = new ProtoPayInvoiceRequest(sessionId.ToString(), invoiceId);
        await _client.PayInvoiceAsync(request, cancellationToken: cancellationToken);
    }

    public async Task CancelInvoiceAsync(Guid sessionId, long invoiceId, CancellationToken cancellationToken)
    {
        var request = new ProtoCancelInvoiceRequest(sessionId.ToString(), invoiceId);
        await _client.CancelInvoiceAsync(request, cancellationToken: cancellationToken);
    }

    public async Task<GetOutgoingInvoicesResponse> GetOutgoingInvoicesAsync(
        GetOutgoingInvoices.Request request,
        CancellationToken cancellationToken)
    {
        IEnumerable<InvoiceStatus> states = request.Statuses.Select(status => status.MapToProto());
        var protoRequest = new ProtoGetOutgoingInvoicesRequest(
            request.SessionId.ToString(),
            request.PageToken,
            request.PageSize,
            states,
            request.PayerIds);
        ProtoGetOutgoingInvoicesResponse response =
            await _client.GetOutgoingInvoicesAsync(protoRequest, cancellationToken: cancellationToken);
        return new GetOutgoingInvoicesResponse(
            response.Invoices.Select(invoice => invoice
                .MapToDto()),
            response.PageToken);
    }

    public async Task<GetIncomingInvoicesResponse> GetIncomingInvoicesAsync(
        GetIncomingInvoices.Request request,
        CancellationToken cancellationToken)
    {
        IEnumerable<InvoiceStatus> states = request.Statuses.Select(status => status.MapToProto());
        var protoRequest = new ProtoGetIncomingInvoicesRequest(
            request.SessionId.ToString(),
            request.PageToken,
            request.PageSize,
            states,
            request.RecipientIds);
        ProtoGetIncomingInvoicesResponse response = await _client
            .GetIncomingInvoicesAsync(protoRequest, cancellationToken: cancellationToken);
        return new GetIncomingInvoicesResponse(
            response.Invoices.Select(invoice => invoice
                .MapToDto()),
            response.PageToken);
    }
}