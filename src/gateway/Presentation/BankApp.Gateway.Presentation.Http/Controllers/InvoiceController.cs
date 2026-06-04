using BankApp.Gateway.Application.Abstractions.Clients;
using BankApp.Gateway.Application.Abstractions.Requests;
using BankApp.Gateway.Application.Abstractions.Requests.Approval;
using BankApp.Gateway.Application.Models;
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
    private readonly IUserClient _userClient;
    private readonly IInvoiceApprovalClient _approvalClient;

    public InvoiceController(IInvoiceClient client, IInvoiceApprovalClient approvalClient, IUserClient userClient)
    {
        _client = client;
        _approvalClient = approvalClient;
        _userClient = userClient;
    }

    [HttpPost("create")]
    [Authorize(Policy = AppFeatures.CreateInvoice)]
    public async Task<ActionResult<long>> CreateInvoiceAsync(
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

    [HttpPost("approve")]
    [Authorize(Policy = AppFeatures.ApproveInvoice)]
    public async Task<ActionResult> ApproveInvoice(
        [FromBody] ApproveInvoiceRequest httpRequest,
        CancellationToken cancellationToken)
    {
        Guid userExternalId = HttpContext.GetCurrentUserId();
        GetUser.Response getUserResponse = await _userClient.GetUserAsync(new GetUser.Request(userExternalId), cancellationToken);
        long userId = getUserResponse.UserId;
        var request = new ApproveInvoice.Request(httpRequest.InvoiceId, userId);
        await _approvalClient.ApproveInvoiceAsync(request, cancellationToken);
        return Ok();
    }

    [HttpPost("decline")]
    [Authorize(Policy = AppFeatures.DeclineInvoice)]
    public async Task<ActionResult> DeclineInvoice(
        [FromBody] DeclineInvoiceRequest httpRequest,
        CancellationToken cancellationToken)
    {
        Guid userExternalId = HttpContext.GetCurrentUserId();
        GetUser.Response getUserResponse = await _userClient.GetUserAsync(new GetUser.Request(userExternalId), cancellationToken);
        long userId = getUserResponse.UserId;
        var request = new DeclineInvoice.Request(httpRequest.InvoiceId, userId);
        await _approvalClient.DeclineInvoiceAsync(request, cancellationToken);
        return Ok();
    }

    [HttpPost("assign")]
    [Authorize(Policy = AppFeatures.AssignUserToInvoice)]
    public async Task<ActionResult> AssignAccountantAsync(
        [FromBody] AssignAccountantRequest request,
        CancellationToken cancellationToken)
    {
        var protoRequest = new AssignAccountant.Request(request.InvoiceId, request.UserId);
        await _approvalClient.AssignAccountantAsync(protoRequest, cancellationToken);
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