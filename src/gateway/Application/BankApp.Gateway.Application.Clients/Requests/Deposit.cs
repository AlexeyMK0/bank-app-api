namespace BankApp.Gateway.Application.Abstractions.Requests;

public static class Deposit
{
    public sealed record Response(decimal Balance);
}