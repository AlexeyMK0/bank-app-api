using BankApp.Gateway.Application.Abstractions.Clients;
using BankApp.Gateway.Application.Abstractions.Requests;
using BankApp.Gateway.Application.Models;
using BankApp.Gateway.Presentation.Http.AuthorizationModels;
using BankApp.Gateway.Presentation.Http.Extensions;
using BankApp.Gateway.Presentation.Http.Operations.Invoices.Requests;
using BankApp.Gateway.Presentation.Http.Operations.Invoices.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using GetOutgoingInvoicesResponse = BankApp.Gateway.Presentation.Http.Operations.Invoices.Responses.GetOutgoingInvoicesResponse;

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
    public async Task<ActionResult<CreateInvoiceResponse>> CreateInvoiceAsync(
        [FromBody] CreateInvoiceRequest httpRequest,
        CancellationToken cancellationToken)
    {
        Guid userId = HttpContext.GetCurrentUserId();

        Activity.Current?.AddUserIdBaggage(userId);
        Activity.Current?.AddAccountIdBaggage(httpRequest.RecipientId);

        CreateInvoice.Response response = await _client.CreateInvoiceAsync(
            userId,
            httpRequest.PayerId,
            httpRequest.RecipientId,
            httpRequest.Amount,
            cancellationToken);
        return Ok(new CreateInvoiceResponse(response.InvoiceId));
    }

    [HttpPost("{id}/cancel")]
    [Authorize(Policy = AppFeatures.CancelInvoice)]
    public async Task<ActionResult> CancelInvoiceAsync(
        [FromRoute] long id,
        CancellationToken cancellationToken)
    {
        Guid userId = HttpContext.GetCurrentUserId();

        Activity.Current?.AddUserIdBaggage(userId);

        await _client.CancelInvoiceAsync(userId, id, cancellationToken);
        return Ok();
    }

    [HttpPost("{id}/pay")]
    [Authorize(Policy = AppFeatures.PayInvoice)]
    public async Task<ActionResult> PayInvoiceAsync(
        [FromRoute] long id,
        CancellationToken cancellationToken)
    {
        Guid userId = HttpContext.GetCurrentUserId();
        await _client.PayInvoiceAsync(userId, id, cancellationToken);
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
            GetInvoicesRequestTypeDto.Incoming,
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
            httpRequest.UserIds ?? [],
            httpRequest.PayerIds ?? [],
            GetInvoicesRequestTypeDto.Outgoing,
            httpRequest.PageSize,
            httpRequest.PageToken);

        GetInvoices.Response response = await _client
            .GetInvoicesAsync(request, cancellationToken);
        var httpResponse = new GetOutgoingInvoicesResponse(
            response.Invoices, response.PageToken);
        return Ok(httpResponse);
    }
}