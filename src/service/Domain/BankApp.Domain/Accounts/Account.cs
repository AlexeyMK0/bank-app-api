using Lab1.Domain.ValueObjects;

namespace Lab1.Domain.Accounts;

public sealed record Account(AccountId Id, PinCode PinCode, Money Balance);