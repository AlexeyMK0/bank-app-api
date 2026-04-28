using BankApp.Domain.ValueObjects;

namespace BankApp.Domain.Accounts;

public sealed record Account(AccountId Id, PinCode PinCode, Money Balance);