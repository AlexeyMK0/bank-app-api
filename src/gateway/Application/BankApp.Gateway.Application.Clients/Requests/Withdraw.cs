namespace BankApp.Gateway.Application.Abstractions.Requests;

public static class Withdraw
{
    public sealed record Response(decimal Balance);
}