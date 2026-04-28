namespace BankApp.Application.Contracts.Sessions.Model;

public sealed record UserSessionDto(Guid SessionId, long AccountId);