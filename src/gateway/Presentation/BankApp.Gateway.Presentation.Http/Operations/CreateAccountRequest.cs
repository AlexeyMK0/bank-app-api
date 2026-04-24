namespace BankApp.Gateway.Presentation.Http.Operations;

public sealed class CreateAccountRequest
{
    public required long AccountOwnerId { get; init; }
}