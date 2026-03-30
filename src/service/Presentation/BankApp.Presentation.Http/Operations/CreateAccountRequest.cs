using System.ComponentModel.DataAnnotations;

namespace Lab1.Presentation.Http.Operations;

public sealed class CreateAccountRequest
{
    [MinLength(1)]
    public required string PinCode { get; init; }

    public required Guid SessionId { get; init; }
}