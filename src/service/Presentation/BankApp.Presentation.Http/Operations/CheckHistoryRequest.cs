using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace Lab1.Presentation.Http.Operations;

public class CheckHistoryRequest
{
    [FromQuery(Name = "pageToken")]
    public string? PageToken { get; set; }

    [FromQuery(Name = "pageSize")]
    [Range(1, int.MaxValue)]
    public required int PageSize { get; set; }

    [FromQuery(Name = "sessionId")]
    public required Guid SessionId { get; set; }
}