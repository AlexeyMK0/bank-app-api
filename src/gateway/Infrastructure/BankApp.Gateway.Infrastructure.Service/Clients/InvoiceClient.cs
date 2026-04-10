using BankApp.Gateway.Application.Abstractions.Clients;
using BankApp.Gateway.Application.Models;
using BankApp.Gateway.Application.Models.Responses;
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

    public async Task<long> CreateInvoiceAsync(
        Guid sessionId,
        long payerId,
        decimal amount,
        CancellationToken cancellationToken)
    {
        var request = new CreateInvoiceRequest(sessionId.ToString(), payerId, new Money { DecimalValue = amount });
        CreateInvoiceResponse response =
            await _client.CreateInvoiceAsync(request, cancellationToken: cancellationToken);
        return response.InvoiceId;
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

    public async Task<GetIncomingInvoicesResponseDto> GetIncomingInvoicesAsync(
        Guid sessionId,
        InvoiceStatusDto[] statuses,
        long[] recipientIds,
        int? pageSize,
        string? pageToken,
        CancellationToken cancellationToken)
    {
        IEnumerable<InvoiceStatus> states = statuses.Select(status => status.MapToGrpc());
        var request = new ProtoGetIncomingInvoicesRequest(sessionId.ToString(), pageToken, pageSize, states, recipientIds);
        GetIncomingInvoicesResponse response =
            await _client.GetIncomingInvoicesAsync(request, cancellationToken: cancellationToken);
        return new GetIncomingInvoicesResponseDto(
            response.Invoices.Select(invoice => invoice
                .MapToDto()),
            response.PageToken);
    }

    public async Task<GetOutgoingInvoicesResponseDto> GetOutgoingInvoicesAsync(
        Guid sessionId,
        InvoiceStatusDto[] statuses,
        long[] payerIds,
        int? pageSize,
        string? pageToken,
        CancellationToken cancellationToken)
    {
        IEnumerable<InvoiceStatus> states = statuses.Select(status => status.MapToGrpc());
        var request = new ProtoGetOutgoingInvoicesRequest(sessionId.ToString(), pageToken, pageSize, states, payerIds);
        GetOutgoingInvoicesResponse response =
            await _client.GetOutgoingInvoicesAsync(request, cancellationToken: cancellationToken);
        return new GetOutgoingInvoicesResponseDto(
            response.Invoices.Select(invoice => invoice
                .MapToDto()),
            response.PageToken);
    }
}