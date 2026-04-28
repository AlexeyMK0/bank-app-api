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

    /*public async Task<ProtoGetInvoicesResponse> GetInvoices(ProtoGetInvoicesRequest request, ServerCallContext context)
    {
        var sessionId = Guid.Parse(request.SessionId);
        InvoiceStatusDto[] states = request
            .InvoiceStatuses.Select(state => state
                .MapToDto())
            .ToArray();
        int pageSize = request.PageSize ?? _defaultPageSize;
        long[] recipientIds = request.RecipientIds.ToArray();
        Console.WriteLine($"Page token is null: {request.PageToken is null}");
        GetIncomingInvoices.PageToken? pageToken
            = request.PageToken is null
                ? null
                : JsonSerializer.Deserialize<GetIncomingInvoices.PageToken>(request.PageToken);
    }*/

    public override async Task<GetIncomingInvoicesResponse> GetIncomingInvoices(
        GetIncomingInvoicesRequest request,
        ServerCallContext context)
    {
        var externalId = Guid.Parse(request.UserExternalId);
        InvoiceStatusDto[] statuses = request
            .InvoiceStatuses.Select(state => state
                .MapToDto())
            .ToArray();
        int pageSize = request.PageSize ?? _defaultPageSize;
        long[] accountIds = request.UserIds.ToArray();
        long[] recipientIds = request.RecipientIds.ToArray();
        Console.WriteLine($"Page token is null: {request.PageToken is null}");
        GetIncomingInvoices.PageToken? pageToken
            = request.PageToken is null
                ? null
                : JsonSerializer.Deserialize<GetIncomingInvoices.PageToken>(request.PageToken);

        var apiRequest = new GetIncomingInvoices.Request(externalId, accountIds, pageToken, pageSize, statuses, recipientIds);

        GetIncomingInvoices.Response result =
            await _invoiceService.GetIncomingInvoicesAsync(apiRequest, context.CancellationToken);
        return result switch
        {
            GetIncomingInvoices.Response.Success success => new ProtoGetIncomingInvoicesResponse
            {
                Invoices = { success.Invoices.Select(invoice => invoice.MapToProto()) },
                PageToken = success.PageToken is null
                    ? null
                    : JsonSerializer.Serialize<GetIncomingInvoices.PageToken>(success.PageToken),
            },
            GetIncomingInvoices.Response.Failure failure => throw new RpcException(
                new Status(StatusCode.InvalidArgument, failure.Message)),
            _ => throw new UnreachableException(),
        };
    }

    public override async Task<GetOutgoingInvoicesResponse> GetOutgoingInvoices(
        GetOutgoingInvoicesRequest request,
        ServerCallContext context)
    {
        var externalId = Guid.Parse(request.UserExternalId);
        InvoiceStatusDto[] statuses = request
            .InvoiceStatuses.Select(state => state
                .MapToDto())
            .ToArray();
        int pageSize = request.PageSize ?? _defaultPageSize;
        long[] payerIds = request.PayerIds.ToArray();
        long[] accountIds = request.UserIds.ToArray();
        GetOutgoingInvoices.PageToken? pageToken
            = request.PageToken is null
                ? null
                : JsonSerializer.Deserialize<GetOutgoingInvoices.PageToken>(request.PageToken);

        var apiRequest = new GetOutgoingInvoices.Request(externalId, accountIds, pageToken, pageSize, statuses, payerIds);

        GetOutgoingInvoices.Response result =
            await _invoiceService.GetOutgoingInvoicesAsync(apiRequest, context.CancellationToken);
        return result switch
        {
            GetOutgoingInvoices.Response.Success success => new ProtoGetOutgoingInvoicesResponse
            {
                Invoices = { success.Invoices.Select(invoice => invoice.MapToProto()) },
                PageToken = success.PageToken is null
                    ? null
                    : JsonSerializer.Serialize<GetOutgoingInvoices.PageToken>(success.PageToken),
            },
            GetOutgoingInvoices.Response.Failure failure => throw new RpcException(
                new Status(StatusCode.InvalidArgument, failure.Message)),
            _ => throw new UnreachableException(),
        };
    }
}