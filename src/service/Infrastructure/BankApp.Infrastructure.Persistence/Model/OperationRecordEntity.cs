using Lab1.Infrastructure.Persistence.Model.PayloadModel;

namespace Lab1.Infrastructure.Persistence.Model;

public record OperationRecordEntity(
    long Id,
    DateTimeOffset Time,
    long AccountId,
    Payload Payload);