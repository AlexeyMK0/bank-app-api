using BankApp.Gateway.Application.Models;

namespace BankApp.Gateway.Application.Abstractions.Requests;

public class CreateAccount
{
    public sealed record Response(AccountDto AccountDto);
}