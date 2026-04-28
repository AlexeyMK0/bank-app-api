using BankApp.Domain.Sessions;
using BankApp.Domain.ValueObjects;

namespace BankApp.Domain.Accounts;

// TODO: make it a class
public sealed record Account(AccountId Id, Money Balance, UserId OwnerUserId);