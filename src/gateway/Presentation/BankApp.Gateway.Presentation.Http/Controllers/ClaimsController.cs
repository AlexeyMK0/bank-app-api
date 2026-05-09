using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BankApp.Gateway.Presentation.Http.Controllers;

public class ClaimsController : ControllerBase
{
    [HttpGet("/api")]
    [Authorize]
    public ActionResult GetData()
    {
        return Ok(HttpContext.User.Claims.Select(x => new KeyValuePair<string, string>(x.Type, x.Value)));
    }
}