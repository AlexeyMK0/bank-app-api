using BankApp.Gateway.Application.Abstractions.Clients;
using BankApp.Gateway.Application.Abstractions.Requests;
using BankApp.Gateway.Presentation.Http.Extensions;
using BankApp.Gateway.Presentation.Http.Operations;
using BankApp.Gateway.Presentation.Http.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BankApp.Gateway.Presentation.Http.Controllers;

[Route("api/operations/history")]
[ApiController]
public class OperationHistoryController : ControllerBase
{
    private readonly IOperationHistoryClient _client;

    public OperationHistoryController(IOperationHistoryClient client)
    {
        _client = client;
    }

    [HttpGet]
    [Authorize]
    public async Task<ActionResult<GetOperationHistoryResponse>> CheckHistory(
        [FromQuery] CheckHistoryRequest httpRequest,
        CancellationToken cancellationToken)
    {
        Guid userId = HttpContext.GetCurrentUserId();
        GetOperationHistory.Response response = await _client
            .GetOperationHistoryAsync(
                userId,
                httpRequest.PageSize,
                httpRequest.PageToken,
                cancellationToken);
        var httpResponse = new GetOperationHistoryResponse(
            response.Operations, response.PageToken);
        return Ok(httpResponse);
    }
}