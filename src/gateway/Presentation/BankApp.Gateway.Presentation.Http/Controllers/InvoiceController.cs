using BankApp.Gateway.Application.Abstractions.Clients;
using BankApp.Gateway.Application.Abstractions.Requests;
using BankApp.Gateway.Application.Models.Responses;
using BankApp.Gateway.Presentation.Http.Operations;
using Microsoft.AspNetCore.Mvc;

namespace BankApp.Gateway.Presentation.Http.Controllers;

[ApiController]
[Route("api/invoices")]
public class InvoiceController : ControllerBase
{
    private readonly IInvoiceClient _client;

    public InvoiceController(IInvoiceClient client)
    {
        _client = client;
    }

    [HttpPost("create")]
    public async Task<ActionResult<long>> CreateInvoiceAsync(
        [FromBody] CreateInvoiceRequest httpRequest,
        CancellationToken cancellationToken)
    {
        CreateInvoice.Response response = await _client.CreateInvoiceAsync(
            httpRequest.SessionId,
            httpRequest.PayerId,
            httpRequest.Amount,
            cancellationToken);
        return Ok(response.InvoiceId);
    }

    [HttpPost("cancel")]
    public async Task<ActionResult> CancelInvoiceAsync(
        [FromBody] CancelInvoiceRequest httpRequest,
        CancellationToken cancellationToken)
    {
        await _client.CancelInvoiceAsync(httpRequest.SessionId, httpRequest.InvoiceId, cancellationToken);
        return Ok();
    }

    [HttpPost("pay")]
    public async Task<ActionResult> PayInvoiceAsync(
        [FromBody] PayInvoiceRequest httpRequest,
        CancellationToken cancellationToken)
    {
        await _client.PayInvoiceAsync(httpRequest.SessionId, httpRequest.InoviceId, cancellationToken);
        return Ok();
    }

    [HttpGet("incoming")]
    public async Task<ActionResult<GetIncomingInvoicesResponse>> GetIncomingInvoicesAsync(
        [FromQuery] GetIncomingInvoicesRequest httpRequest,
        CancellationToken cancellationToken)
    {
        var request = new GetIncomingInvoices.Request(
            httpRequest.SessionId,
            httpRequest.InvoiceStatuses ?? [],
            httpRequest.RecipientIds ?? [],
            httpRequest.PageSize,
            httpRequest.PageToken);

        GetIncomingInvoicesResponse response = await _client
            .GetIncomingInvoicesAsync(request, cancellationToken);
        return Ok(response);
    }

    [HttpGet("outgoing")]
    public async Task<ActionResult<GetOutgoingInvoicesResponse>> GetOutgoingInvoicesAsync(
        [FromQuery] GetOutgoingInvoicesRequest httpRequest,
        CancellationToken cancellationToken)
    {
        var request = new GetOutgoingInvoices.Request(
            httpRequest.SessionId,
            httpRequest.InvoiceStatuses ?? [],
            httpRequest.PayerIds ?? [],
            httpRequest.PageSize,
            httpRequest.PageToken);

        GetOutgoingInvoicesResponse response = await _client
            .GetOutgoingInvoicesAsync(request, cancellationToken);
        return Ok(response);
    }
}