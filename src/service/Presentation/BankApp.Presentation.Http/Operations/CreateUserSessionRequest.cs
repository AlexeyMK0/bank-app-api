using System.ComponentModel.DataAnnotations;

namespace Lab1.Presentation.Http.Operations;

public sealed class CreateUserSessionRequest
{
    [Range(1, int.MaxValue)]
    public required long AccountId { get; init; }

    [MinLength(1)]
    public required string PinCode { get; init; }
}