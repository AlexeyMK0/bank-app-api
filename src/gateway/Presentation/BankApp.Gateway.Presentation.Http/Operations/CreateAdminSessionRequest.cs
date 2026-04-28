using System.ComponentModel.DataAnnotations;

namespace BankApp.Gateway.Presentation.Http.Operations;

public sealed class CreateAdminSessionRequest
{
    [Length(1, int.MaxValue)]
    public required string SystemPassword { get; init; }
}