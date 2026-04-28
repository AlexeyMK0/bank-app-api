using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace BankApp.Gateway.Presentation.Http.Operations;

public class CheckHistoryRequest
{
    [FromQuery(Name = "pageToken")]
    public string? PageToken { get; set; }

    [FromQuery(Name = "pageSize")]
    [Range(1, int.MaxValue)]
    public int? PageSize { get; set; }

    [FromQuery(Name = "sessionId")]
    public required Guid SessionId { get; set; }
}