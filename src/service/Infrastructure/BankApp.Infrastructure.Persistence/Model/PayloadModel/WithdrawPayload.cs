namespace BankApp.Infrastructure.Persistence.Model.PayloadModel;

internal sealed record WithdrawPayload(decimal Amount) : Payload;