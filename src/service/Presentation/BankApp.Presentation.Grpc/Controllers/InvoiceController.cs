using BankApp.Grpc;
using Contracts.Invoices;
using Contracts.Invoices.Operations;
using Grpc.Core;
using System.Diagnostics;

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

    public override Task<GetIncomingInvoicesResponse> GetIncomingInvoices(GetIncomingInvoicesRequest request, ServerCallContext context)
    {
        /*var sessionId = Guid.Parse(request.SessionId);
        request.InvoiceStatus;
        int pageSize = request.PageSize;
        GetIncomingInvoices.PageToken? pageToken = null;
        if (request.PageToken is not null)
        {
            if (int.TryParse(request.PageToken, out int invoiceId) is false)
            {
                return new GetIncomingInvoicesResponse
                    { Failure = new GetIncomingInvoicesResponse.Types.Failure("Bad page token") },
            }
        }
            = request.PageToken is null
            ? null
            : new GetIncomingInvoices.PageToken(int.Parse(request.PageToken));
        request.RecipientIds;
        var apiRequest = new GetIncomingInvoices.Request(sessionId, );

        GetIncomingInvoices.Response result = await _invoiceService.GetIncomingInvoicesAsync(apiRequest, context.CancellationToken);
        return result switch
        {
            GetIncomingInvoices.Response.Success success => new ProtoGetIncomingInvoicesResponse
                { Success = new ProtoGetIncomingInvoicesResponse.Types.Success() },
            GetIncomingInvoices.Response.Failure failure => new GetIncomingInvoicesResponse
                { Failure = new GetIncomingInvoicesResponse.Types.Failure(failure.Message) },
            _ => throw new UnreachableException(),
        };*/
        return base.GetIncomingInvoices(request, context);
    }

    public override Task<GetOutgoingInvoicesResponse> GetOutgoingInvoices(GetOutgoingInvoicesRequest request, ServerCallContext context)
    {
        return base.GetOutgoingInvoices(request, context);
    }
}