using BankApp.Gateway.Application.Models;
using BankApp.Gateway.Application.Models.Responses;

namespace BankApp.Gateway.Application.Abstractions.Clients;

public interface IInvoiceClient
{
    Task<long> CreateInvoiceAsync(Guid sessionId, long payerId, decimal amount, CancellationToken cancellationToken);

    Task PayInvoiceAsync(Guid sessionId, long invoiceId, CancellationToken cancellationToken);

    Task CancelInvoiceAsync(Guid sessionId, long invoiceId, CancellationToken cancellationToken);

    Task<GetIncomingInvoicesResponseDto> GetIncomingInvoicesAsync(
        Guid sessionId,
        InvoiceStatusDto[] statuses,
        long[] recipientIds,
        int? pageSize,
        string? pageToken,
        CancellationToken cancellationToken);

    Task<GetOutgoingInvoicesResponseDto> GetOutgoingInvoicesAsync(
        Guid sessionId,
        InvoiceStatusDto[] statuses,
        long[] payerIds,
        int? pageSize,
        string? pageToken,
        CancellationToken cancellationToken);
}