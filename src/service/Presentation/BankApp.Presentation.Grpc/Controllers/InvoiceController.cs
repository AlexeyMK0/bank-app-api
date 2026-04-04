using BankApp.Grpc;
using BankApp.Presentation.Grpc.Mappers;
using Contracts.Invoices;
using Contracts.Invoices.Operations;
using Grpc.Core;
using System.Diagnostics;
using System.Text.Json;

namespace BankApp.Presentation.Grpc.Controllers;

public class InvoiceController : InvoiceService.InvoiceServiceBase
{
    private readonly IInvoiceService _invoiceService;

    public InvoiceController(IInvoiceService invoiceService)
    {
        _invoiceService = invoiceService;
    }

    public override async Task<CreateInvoiceResponse> CreateInvoice(CreateInvoiceRequest request, ServerCallContext context)
    {
        var sessionId = Guid.Parse(request.SessionId);
        long payerId = request.PayerId;
        decimal amount = request.Amount.DecimalValue;
        var apiRequest = new CreateInvoice.Request(sessionId, payerId, amount);

        CreateInvoice.Response result = await _invoiceService.CreateInvoiceAsync(apiRequest, context.CancellationToken);
        return result switch
        {
            CreateInvoice.Response.Success success => new ProtoCreateInvoiceResponse
            {
                Success = new CreateInvoiceResponse.Types.Success(success.InvoiceId),
            },
            CreateInvoice.Response.Failure failure => new CreateInvoiceResponse
                { Failure = new CreateInvoiceResponse.Types.Failure(failure.Message) },
            _ => throw new UnreachableException(),
        };
    }

    public override async Task<CancelInvoiceResponse> CancelInvoice(CancelInvoiceRequest request, ServerCallContext context)
    {
        var sessionId = Guid.Parse(request.SessionId);
        long invoiceId = request.InvoiceId;
        var apiRequest = new CancelInvoice.Request(sessionId, invoiceId);

        CancelInvoice.Response result = await _invoiceService.CancelInvoiceAsync(apiRequest, context.CancellationToken);
        return result switch
        {
            CancelInvoice.Response.Success success => new ProtoCancelInvoiceResponse
                { Success = new CancelInvoiceResponse.Types.Success() },
            CancelInvoice.Response.Failure failure => new CancelInvoiceResponse
                { Failure = new CancelInvoiceResponse.Types.Failure(failure.Message) },
            _ => throw new UnreachableException(),
        };
    }

    public override async Task<PayInvoiceResponse> PayInvoice(PayInvoiceRequest request, ServerCallContext context)
    {
        var sessionId = Guid.Parse(request.SessionId);
        long invoiceId = request.InvoiceId;
        var apiRequest = new PayInvoice.Request(sessionId, invoiceId);

        PayInvoice.Response result = await _invoiceService.PayInvoiceAsync(apiRequest, context.CancellationToken);
        return result switch
        {
            PayInvoice.Response.Success success => new ProtoPayInvoiceResponse
                { Success = new PayInvoiceResponse.Types.Success() },
            PayInvoice.Response.Failure failure => new PayInvoiceResponse
                { Failure = new PayInvoiceResponse.Types.Failure(failure.Message) },
            _ => throw new UnreachableException(),
        };
    }

    public override async Task<GetIncomingInvoicesResponse> GetIncomingInvoices(GetIncomingInvoicesRequest request, ServerCallContext context)
    {
        var sessionId = Guid.Parse(request.SessionId);
        InvoiceStateDto state = request.InvoiceStatus.MapToDto();
        int pageSize = request.PageSize;
        long[] recipientIds = request.RecipientIds.ToArray();
        GetIncomingInvoices.PageToken? pageToken
            = request.PageToken is null
                ? null
                : JsonSerializer.Deserialize<GetIncomingInvoices.PageToken>(request.PageToken);

        var apiRequest = new GetIncomingInvoices.Request(sessionId, pageToken, pageSize, state, recipientIds);

        GetIncomingInvoices.Response result = await _invoiceService.GetIncomingInvoicesAsync(apiRequest, context.CancellationToken);
        return result switch
        {
            GetIncomingInvoices.Response.Success success => new ProtoGetIncomingInvoicesResponse
            {
                Success = new GetIncomingInvoicesResponse.Types.Success
                {
                    Invoices = { success.Invoices.Select(invoice => invoice.MapToGrpc()) },
                    PageToken = success.PageToken is null
                        ? null
                        : JsonSerializer.Serialize<GetIncomingInvoices.PageToken>(success.PageToken),
                },
            },
            GetIncomingInvoices.Response.Failure failure => new GetIncomingInvoicesResponse
                { Failure = new GetIncomingInvoicesResponse.Types.Failure(failure.Message) },
            _ => throw new UnreachableException(),
        };
    }

    public override async Task<GetOutgoingInvoicesResponse> GetOutgoingInvoices(GetOutgoingInvoicesRequest request, ServerCallContext context)
    {
        var sessionId = Guid.Parse(request.SessionId);
        InvoiceStateDto state = request.InvoiceStatus.MapToDto();
        int pageSize = request.PageSize;
        long[] payerIds = request.PayerIds.ToArray();
        GetOutgoingInvoices.PageToken? pageToken
            = request.PageToken is null
                ? null
                : JsonSerializer.Deserialize<GetOutgoingInvoices.PageToken>(request.PageToken);

        var apiRequest = new GetOutgoingInvoices.Request(sessionId, pageToken, pageSize, state, payerIds);

        GetOutgoingInvoices.Response result = await _invoiceService.GetOutgoingInvoicesAsync(apiRequest, context.CancellationToken);
        return result switch
        {
            GetOutgoingInvoices.Response.Success success => new ProtoGetOutgoingInvoicesResponse
            {
                Success = new GetOutgoingInvoicesResponse.Types.Success
                {
                    Invoices = { success.Invoices.Select(invoice => invoice.MapToGrpc()) },
                    PageToken = success.PageToken is null
                        ? null
                        : JsonSerializer.Serialize<GetOutgoingInvoices.PageToken>(success.PageToken),
                },
            },
            GetOutgoingInvoices.Response.Failure failure => new GetOutgoingInvoicesResponse
                { Failure = new GetOutgoingInvoicesResponse.Types.Failure(failure.Message) },
            _ => throw new UnreachableException(),
        };
    }
}