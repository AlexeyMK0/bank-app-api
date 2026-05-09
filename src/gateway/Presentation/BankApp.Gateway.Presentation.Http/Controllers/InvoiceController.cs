using BankApp.Gateway.Application.Abstractions.Clients;
using BankApp.Gateway.Application.Abstractions.Requests;
using BankApp.Gateway.Presentation.Http.AuthorizationModels;
using BankApp.Gateway.Presentation.Http.Extensions;
using BankApp.Gateway.Presentation.Http.Operations;
using BankApp.Gateway.Presentation.Http.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
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

    [HttpPost("create")]
    [Authorize(Policy = AppFeatures.CreateInvoice)]
    public async Task<ActionResult<long>> CreateInvoiceAsync(
        [FromBody] CreateInvoiceRequest httpRequest,
        CancellationToken cancellationToken)
    {
        Guid userId = HttpContext.GetCurrentUserId();

        Activity.Current?.AddUserIdBaggage(userId);
        Activity.Current?.AddAccountIdBaggage(httpRequest.RecepientId);

        CreateInvoice.Response response = await _client.CreateInvoiceAsync(
            userId,
            httpRequest.PayerId,
            httpRequest.RecepientId,
            httpRequest.Amount,
            cancellationToken);
        return Ok(response.InvoiceId);
    }

    [HttpPost("cancel")]
    [Authorize(Policy = AppFeatures.CancelInvoice)]
    public async Task<ActionResult> CancelInvoiceAsync(
        [FromBody] CancelInvoiceRequest httpRequest,
        CancellationToken cancellationToken)
    {
        Guid userId = HttpContext.GetCurrentUserId();

        Activity.Current?.AddUserIdBaggage(userId);

        await _client.CancelInvoiceAsync(userId, httpRequest.InvoiceId, cancellationToken);
        return Ok();
    }

    [HttpPost("pay")]
    [Authorize(Policy = AppFeatures.PayInvoice)]
    public async Task<ActionResult> PayInvoiceAsync(
        [FromBody] PayInvoiceRequest httpRequest,
        CancellationToken cancellationToken)
    {
        Guid userId = HttpContext.GetCurrentUserId();
        await _client.PayInvoiceAsync(userId, httpRequest.InoviceId, cancellationToken);
        return Ok();
    }

    [HttpGet("incoming")]
    [Authorize(Policy = AppFeatures.ReadInvoice)]
    public async Task<ActionResult<GetIncomingInvoicesResponse>> GetIncomingInvoicesAsync(
        [FromQuery] GetIncomingInvoicesRequest httpRequest,
        CancellationToken cancellationToken)
    {
        Guid userId = HttpContext.GetCurrentUserId();

        Activity.Current?.AddUserIdBaggage(userId);

        var request = new GetInvoices.Request(
            userId,
            httpRequest.InvoiceStatuses ?? [],
            httpRequest.UserIds ?? [],
            httpRequest.RecipientIds ?? [],
            httpRequest.PageSize,
            httpRequest.PageToken);

        GetInvoices.Response response = await _client
            .GetInvoicesAsync(request, cancellationToken);
        var httpResponse = new GetIncomingInvoicesResponse(
            response.Invoices, response.PageToken);
        return Ok(httpResponse);
    }

    [HttpGet("outgoing")]
    [Authorize(Policy = AppFeatures.ReadInvoice)]
    public async Task<ActionResult<GetOutgoingInvoicesResponse>> GetOutgoingInvoicesAsync(
        [FromQuery] GetOutgoingInvoicesRequest httpRequest,
        CancellationToken cancellationToken)
    {
        Guid userId = HttpContext.GetCurrentUserId();

        Activity.Current?.AddUserIdBaggage(userId);

        var request = new GetInvoices.Request(
            userId,
            httpRequest.InvoiceStatuses ?? [],
            httpRequest.PayerIds ?? [],
            httpRequest.UserIds ?? [],
            httpRequest.PageSize,
            httpRequest.PageToken);

        GetInvoices.Response response = await _client
            .GetInvoicesAsync(request, cancellationToken);
        var httpResponse = new GetOutgoingInvoicesResponse(
            response.Invoices, response.PageToken);
        return Ok(httpResponse);
    }
}