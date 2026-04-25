namespace BankApp.Gateway.Application.Abstractions.Requests;

public static class GetBalance
{
    public sealed record Response(decimal Balance);
}