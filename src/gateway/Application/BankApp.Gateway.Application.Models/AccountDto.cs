namespace BankApp.Gateway.Application.Models;

public sealed record AccountDto(long AccountId, decimal Balance, long UserId);