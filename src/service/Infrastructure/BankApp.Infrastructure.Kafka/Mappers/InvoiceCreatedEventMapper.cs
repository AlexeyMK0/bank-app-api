using BankApp.Application.Abstractions.Events;
using Google.Type;
using Itmo.Dev.Platform.Kafka.Producer;

namespace BankApp.Infrastructure.Kafka.Mappers;

public static class InvoiceCreatedEventMapper
{
    public static KafkaProducerMessage<ProtoInvoiceCreationKey, ProtoInvoiceCreationValue> MapToMessage(
        this InvoiceCreatedEvent @event)
    {
        return new KafkaProducerMessage<ProtoInvoiceCreationKey, ProtoInvoiceCreationValue>(
            new ProtoInvoiceCreationKey(@event.InvoiceId.Value),
            new ProtoInvoiceCreationValue(
                @event.InvoiceId.Value,
                @event.RecipientId.Value,
                @event.PayerId.Value,
                new Money { DecimalValue = @event.Amount.Value }));
    }
}