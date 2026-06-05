using BankApp.Gateway.Application.Contracts;
using BankApp.Gateway.Presentation.Http.AuthorizationModels;
using BankApp.Gateway.Presentation.Http.Extensions;
using BankApp.Gateway.Presentation.Http.Operations.Invoices.Requests;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BankApp.Gateway.Presentation.Http.Controllers;

[ApiController]
[Route("api/invoices/{id}")]
public class InvoiceApprovalController : ControllerBase
{
    private readonly IInvoiceApprovalService _approvalService;

    public InvoiceApprovalController(IInvoiceApprovalService approvalService)
    {
        _approvalService = approvalService;
    }

    [HttpPost("approve")]
    [Authorize(Policy = AppFeatures.ApproveInvoice)]
    public async Task<ActionResult> ApproveInvoiceAsync(
        [FromRoute] long id,
        CancellationToken cancellationToken)
    {
        Guid userExternalId = HttpContext.GetCurrentUserId();
        await _approvalService.ApproveInvoiceAsync(userExternalId, id, cancellationToken);
        return Ok();
    }

    [HttpPost("decline")]
    [Authorize(Policy = AppFeatures.DeclineInvoice)]
    public async Task<ActionResult> DeclineInvoiceAsync(
        [FromRoute] long id,
        CancellationToken cancellationToken)
    {
        Guid userExternalId = HttpContext.GetCurrentUserId();
        await _approvalService.DeclineInvoiceAsync(userExternalId, id, cancellationToken);
        return Ok();
    }

    [HttpPost("assign")]
    [Authorize(Policy = AppFeatures.AssignUserToInvoice)]
    public async Task<ActionResult> AssignAccountantAsync(
        [FromRoute] long id,
        [FromBody] AssignAccountantRequest httpRequest,
        CancellationToken cancellationToken)
    {
        Guid userExternalId = HttpContext.GetCurrentUserId();
        await _approvalService.AssignAccountantAsync(
            userExternalId,
            httpRequest.UserId,
            id,
            cancellationToken);
        return Ok();
    }
}