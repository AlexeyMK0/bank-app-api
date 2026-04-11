namespace BankApp.Gateway.Application.Abstractions.Requests;

public class Deposit
{
    public sealed record Response(decimal Balance);
}