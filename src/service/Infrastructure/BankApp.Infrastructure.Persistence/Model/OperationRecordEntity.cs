using BankApp.Infrastructure.Persistence.Model.PayloadModel;

namespace BankApp.Infrastructure.Persistence.Model;

public sealed record OperationRecordEntity(
    long Id,
    DateTimeOffset Time,
    long AccountId,
    Payload Payload);