namespace BankApp.Gateway.Application.Abstractions.Requests;

public class GetBalance
{
    public sealed record Response(decimal Balance);
}