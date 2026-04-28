using BankApp.Gateway.Application.Abstractions.Clients;
using BankApp.Gateway.Application.Models.Responses;
using BankApp.Gateway.Presentation.Http.Operations;
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
    public async Task<ActionResult<GetOperationHistoryResponse>> CheckHistory(
        [FromQuery] CheckHistoryRequest httpRequest,
        CancellationToken cancellationToken)
    {
        GetOperationHistoryResponse response = await _client
            .GetOperationHistoryAsync(
                httpRequest.SessionId,
                httpRequest.PageSize,
                httpRequest.PageToken,
                cancellationToken);
        return Ok(response);
    }
}