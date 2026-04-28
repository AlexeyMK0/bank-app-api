namespace BankApp.Gateway.Application.Abstractions.Requests;

public class Withdraw
{
    public sealed record Response(decimal Balance);
}