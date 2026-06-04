using BankApp.Application.Abstractions.Events;
using Itmo.Dev.Platform.Kafka.Producer;

namespace BankApp.Infrastructure.Kafka.Mappers;

public static class AccountCreatedEventMapper
{
    public static KafkaProducerMessage<ProtoAccountCreationKey, ProtoAccountCreationValue> MapToMessage(
        this AccountCreatedEvent @event)
    {
        return new KafkaProducerMessage<ProtoAccountCreationKey, ProtoAccountCreationValue>(
            new ProtoAccountCreationKey(@event.AccountId.Value),
            new ProtoAccountCreationValue(
                @event.UserId.Value,
                @event.AccountId.Value,
                @event.AccountType.MapToProto()));
    }
}