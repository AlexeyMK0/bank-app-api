using BankApp.Gateway.Application.Abstractions.Clients;
using BankApp.Gateway.Application.Abstractions.Requests.Approval;
using Invoices.Grpc.Contracts;
using Microsoft.Extensions.Logging;

namespace BankApp.Gateway.Infrastructure.Lab5Tools;

public class InvoiceApprovalClient : IInvoiceApprovalClient
{
    private readonly InvoiceService.InvoiceServiceClient _client;

    private readonly ILogger<InvoiceApprovalClient> _logger;

    public InvoiceApprovalClient(InvoiceService.InvoiceServiceClient client, ILogger<InvoiceApprovalClient> logger)
    {
        _client = client;
        _logger = logger;
    }

    public async Task ApproveInvoiceAsync(ApproveInvoice.Request request, CancellationToken cancellationToken)
    {
        var protoRequest = new ProtoApproveInvoiceRequest(request.InvoiceId, request.UserId);

        await _client.ApproveInvoiceAsync(protoRequest, cancellationToken: cancellationToken);
    }

    public async Task DeclineInvoiceAsync(DeclineInvoice.Request request, CancellationToken cancellationToken)
    {
        var protoRequest = new ProtoDeclineInvoiceRequest(request.InvoiceId, request.UserId);
        await _client.DeclineInvoiceAsync(protoRequest, cancellationToken: cancellationToken);
    }

    public async Task AssignAccountantAsync(AssignAccountant.Request request, CancellationToken cancellationToken)
    {
        try
        {
            var protoRequest = new ProtoAssignAccountantRequest(request.InvoiceId, request.AccountantId);
            await _client.AssignAccountantAsync(protoRequest, cancellationToken: cancellationToken);
            _logger.LogInformation("Successfully called AssignAccountantAsync");
        }
        catch (Exception e)
        {
            _logger.LogError(e.Message);
        }
    }
}