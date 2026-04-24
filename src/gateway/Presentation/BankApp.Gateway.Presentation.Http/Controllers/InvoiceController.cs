using BankApp.Gateway.Application.Abstractions.Clients;
using BankApp.Gateway.Application.Abstractions.Requests;
using BankApp.Gateway.Presentation.Http.Extensions;
using BankApp.Gateway.Presentation.Http.Operations;
using BankApp.Gateway.Presentation.Http.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using GetOutgoingInvoicesResponse = BankApp.Gateway.Presentation.Http.Responses.GetOutgoingInvoicesResponse;

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

    // TODO: is it ok to create invoice without recipientId
    [HttpPost("create")]
    [Authorize]
    public async Task<ActionResult<long>> CreateInvoiceAsync(
        [FromBody] CreateInvoiceRequest httpRequest,
        CancellationToken cancellationToken)
    {
        Guid userId = HttpContext.GetCurrentUserId();
        CreateInvoice.Response response = await _client.CreateInvoiceAsync(
            userId,
            httpRequest.PayerId,
            httpRequest.Amount,
            cancellationToken);
        return Ok(response.InvoiceId);
    }

    [HttpPost("cancel")]
    [Authorize]
    public async Task<ActionResult> CancelInvoiceAsync(
        [FromBody] CancelInvoiceRequest httpRequest,
        CancellationToken cancellationToken)
    {
        Guid userId = HttpContext.GetCurrentUserId();
        await _client.CancelInvoiceAsync(userId, httpRequest.InvoiceId, cancellationToken);
        return Ok();
    }

    [HttpPost("pay")]
    [Authorize]
    public async Task<ActionResult> PayInvoiceAsync(
        [FromBody] PayInvoiceRequest httpRequest,
        CancellationToken cancellationToken)
    {
        Guid userId = HttpContext.GetCurrentUserId();
        await _client.PayInvoiceAsync(userId, httpRequest.InoviceId, cancellationToken);
        return Ok();
    }

    [HttpGet("incoming")]
    [Authorize]
    public async Task<ActionResult<GetIncomingInvoicesResponse>> GetIncomingInvoicesAsync(
        [FromQuery] GetIncomingInvoicesRequest httpRequest,
        CancellationToken cancellationToken)
    {
        Guid userId = HttpContext.GetCurrentUserId();
        var request = new GetIncomingInvoices.Request(
            userId,
            httpRequest.InvoiceStatuses ?? [],
            httpRequest.RecipientIds ?? [],
            httpRequest.PageSize,
            httpRequest.PageToken);

        GetIncomingInvoices.Response response = await _client
            .GetIncomingInvoicesAsync(request, cancellationToken);
        var httpResponse = new GetIncomingInvoicesResponse(
            response.Invoices, response.PageToken);
        return Ok(httpResponse);
    }

    [HttpGet("outgoing")]
    [Authorize]
    public async Task<ActionResult<GetOutgoingInvoicesResponse>> GetOutgoingInvoicesAsync(
        [FromQuery] GetOutgoingInvoicesRequest httpRequest,
        CancellationToken cancellationToken)
    {
        Guid userId = HttpContext.GetCurrentUserId();
        var request = new GetOutgoingInvoices.Request(
            userId,
            httpRequest.InvoiceStatuses ?? [],
            httpRequest.PayerIds ?? [],
            httpRequest.PageSize,
            httpRequest.PageToken);

        GetOutgoingInvoices.Response response = await _client
            .GetOutgoingInvoicesAsync(request, cancellationToken);
        var httpResponse = new GetOutgoingInvoicesResponse(
            response.Invoices, response.PageToken);
        return Ok(httpResponse);
    }
}