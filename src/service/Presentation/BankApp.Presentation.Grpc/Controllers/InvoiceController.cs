using BankApp.Application.Contracts.Invoices;
using BankApp.Application.Contracts.Invoices.Operations;
using BankApp.Grpc;
using BankApp.Presentation.Grpc.Mappers;
using BankApp.Presentation.Grpc.Options;
using Grpc.Core;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using System.Text.Json;

namespace BankApp.Presentation.Grpc.Controllers;

public class InvoiceController : InvoiceService.InvoiceServiceBase
{
    private readonly IInvoiceService _invoiceService;

    private readonly int _defaultPageSize;

    public InvoiceController(IInvoiceService invoiceService, IOptions<InvoiceControllerOptions> options)
    {
        _invoiceService = invoiceService;
        _defaultPageSize = options.Value.DefaultPageSize;
    }

    public override async Task<ProtoCreateInvoiceResponse> CreateInvoice(
        ProtoCreateInvoiceRequest request,
        ServerCallContext context)
    {
        var externalId = Guid.Parse(request.UserExternalId);
        decimal amount = request.Amount.DecimalValue;

        var apiRequest = new CreateInvoice.Request(externalId, request.PayerAccountId, request.RecipientAccountId, amount);

        CreateInvoice.Response result = await _invoiceService.CreateInvoiceAsync(apiRequest, context.CancellationToken);
        return result switch
        {
            CreateInvoice.Response.Success success => new ProtoCreateInvoiceResponse(success.InvoiceId),
            CreateInvoice.Response.Failure failure => throw new RpcException(
                new Status(StatusCode.InvalidArgument, failure.Message)),
            _ => throw new UnreachableException(),
        };
    }

    public override async Task<CancelInvoiceResponse> CancelInvoice(
        CancelInvoiceRequest request,
        ServerCallContext context)
    {
        var externalId = Guid.Parse(request.UserExternalId);
        long invoiceId = request.InvoiceId;

        var apiRequest = new CancelInvoice.Request(externalId, invoiceId);

        CancelInvoice.Response result = await _invoiceService.CancelInvoiceAsync(apiRequest, context.CancellationToken);
        return result switch
        {
            CancelInvoice.Response.Success success => new ProtoCancelInvoiceResponse(),
            CancelInvoice.Response.Failure failure => throw new RpcException(
                new Status(StatusCode.InvalidArgument, failure.Message)),
            _ => throw new UnreachableException(),
        };
    }

    public override async Task<PayInvoiceResponse> PayInvoice(PayInvoiceRequest request, ServerCallContext context)
    {
        var externalId = Guid.Parse(request.UserExternalId);
        long invoiceId = request.InvoiceId;
        var apiRequest = new PayInvoice.Request(externalId, invoiceId);

        PayInvoice.Response result = await _invoiceService.PayInvoiceAsync(apiRequest, context.CancellationToken);
        return result switch
        {
            PayInvoice.Response.Success success => new ProtoPayInvoiceResponse(),
            PayInvoice.Response.Failure failure => throw new RpcException(
                new Status(StatusCode.InvalidArgument, failure.Message)),
            _ => throw new UnreachableException(),
        };
    }

    public override async Task<ProtoGetInvoicesResponse> GetInvoices(ProtoGetInvoicesRequest request, ServerCallContext context)
    {
        var externalId = Guid.Parse(request.UserExternalId);
        InvoiceStatusDto[] states = request
            .InvoiceStatuses.Select(state => state
                .MapToDto())
            .ToArray();
        int pageSize = request.PageSize ?? _defaultPageSize;
        long[] recipientIds = request.RecipientIds.ToArray();
        long[] payerIds = request.PayerIds.ToArray();
        GetInvoices.PageToken? pageToken
            = request.PageToken is null
                ? null
                : JsonSerializer.Deserialize<GetInvoices.PageToken>(request.PageToken);

        var apiRequest = new GetInvoices.Request(externalId, pageToken, pageSize, states, payerIds, recipientIds);
        GetInvoices.Response response = await _invoiceService.GetInvoicesAsync(apiRequest, context.CancellationToken);
        return response switch
        {
            GetInvoices.Response.Success success => new ProtoGetInvoicesResponse
            {
                Invoices = { success.Invoices.Select(invoice => invoice.MapToProto()) },
                PageToken = success.PageToken is null
                    ? null
                    : JsonSerializer.Serialize<GetInvoices.PageToken>(success.PageToken),
            },
            GetInvoices.Response.Failure failure => throw new RpcException(
                new Status(StatusCode.InvalidArgument, failure.Message)),
            _ => throw new UnreachableException(),
        };
    }
}