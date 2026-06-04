using BankApp.Application.Abstractions.Events;
using BankApp.Application.Abstractions.Publishers;
using BankApp.Infrastructure.Kafka.Mappers;
using Itmo.Dev.Platform.Kafka.Producer;

namespace BankApp.Infrastructure.Kafka.Publishers;

public sealed class AccountCreatedEventPublisher : IAccountCreatedEventPublisher
{
    private readonly IKafkaMessageProducer<ProtoAccountCreationKey, ProtoAccountCreationValue> _producer;

    public AccountCreatedEventPublisher(IKafkaMessageProducer<ProtoAccountCreationKey, ProtoAccountCreationValue> producer)
    {
        _producer = producer;
    }

    public async Task PublishAsync(IReadOnlyList<AccountCreatedEvent> approvalInvoiceEvents, CancellationToken cancellationToken)
    {
        IAsyncEnumerable<KafkaProducerMessage<ProtoAccountCreationKey, ProtoAccountCreationValue>> events = approvalInvoiceEvents
            .Select(ev => ev.MapToMessage())
            .ToAsyncEnumerable();

        await _producer.ProduceAsync(events, cancellationToken);
    }
}