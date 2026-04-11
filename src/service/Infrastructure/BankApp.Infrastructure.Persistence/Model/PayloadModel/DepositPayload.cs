namespace BankApp.Infrastructure.Persistence.Model.PayloadModel;

internal sealed record DepositPayload(decimal Amount) : Payload;