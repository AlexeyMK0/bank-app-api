using BankApp.Gateway.Application.Abstractions.Clients;
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
        long createdInvoiceId = await _client.CreateInvoiceAsync(
            httpRequest.SessionId,
            httpRequest.PayerId,
            httpRequest.Amount,
            cancellationToken);
        return Ok(createdInvoiceId);
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
    public async Task<ActionResult<GetIncomingInvoicesResponseDto>> GetIncomingInvoicesAsync(
        [FromQuery] GetIncomingInvoicesRequest httpRequest,
        CancellationToken cancellationToken)
    {
        GetIncomingInvoicesResponseDto response = await _client
            .GetIncomingInvoicesAsync(
                httpRequest.SessionId,
                httpRequest.InvoiceStatuses ?? [],
                httpRequest.RecipientIds ?? [],
                httpRequest.PageSize,
                httpRequest.PageToken,
                cancellationToken);
        return Ok(response);
    }

    [HttpGet("outgoing")]
    public async Task<ActionResult<GetOutgoingInvoicesResponseDto>> GetOutgoingInvoicesAsync(
        [FromQuery] GetOutgoingInvoicesRequest httpRequest,
        CancellationToken cancellationToken)
    {
        GetOutgoingInvoicesResponseDto response = await _client
            .GetOutgoingInvoicesAsync(
                httpRequest.SessionId,
                httpRequest.InvoiceStatuses ?? [],
                httpRequest.PayerIds ?? [],
                httpRequest.PageSize,
                httpRequest.PageToken,
                cancellationToken);
        return Ok(response);
    }
}