using Lab1.Domain.Accounts;

namespace Lab1.Domain.Sessions;

public sealed record UserSession(SessionId SessionGuid, AccountId AccountId);