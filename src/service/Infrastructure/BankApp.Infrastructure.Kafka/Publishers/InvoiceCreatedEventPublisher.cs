using BankApp.Application.Abstractions.Events;
using BankApp.Application.Abstractions.Publishers;
using BankApp.Infrastructure.Kafka.Mappers;
using Itmo.Dev.Platform.Kafka.Producer;

namespace BankApp.Infrastructure.Kafka.Publishers;

public sealed class InvoiceCreatedEventPublisher : IInvoiceCreatedEventPublisher
{
    private readonly IKafkaMessageProducer<ProtoInvoiceCreationKey, ProtoInvoiceCreationValue> _producer;

    public InvoiceCreatedEventPublisher(IKafkaMessageProducer<ProtoInvoiceCreationKey, ProtoInvoiceCreationValue> producer)
    {
        _producer = producer;
    }

    public async Task PublishAsync(IReadOnlyCollection<InvoiceCreatedEvent> events, CancellationToken cancellationToken)
    {
        IAsyncEnumerable<KafkaProducerMessage<ProtoInvoiceCreationKey, ProtoInvoiceCreationValue>> eventsToPublish =
            events.Select(ev => ev.MapToMessage())
                .ToAsyncEnumerable();

        await _producer.ProduceAsync(eventsToPublish, cancellationToken);
    }
}