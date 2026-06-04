using Itmo.Dev.Platform.Kafka.Configuration;
using Itmo.Dev.Platform.Kafka.Extensions;
using Microsoft.Extensions.Configuration;

namespace BankApp.Infrastructure.Kafka;

public static class KafkaConfigurationExtension
{
    public static IKafkaConfigurationBuilder AddInfrastructureProducers(
        this IKafkaConfigurationBuilder kafka,
        IConfiguration configuration)
    {
        const string producerKey = "Infrastructure:Kafka:Producers";
        configuration = configuration.GetSection(producerKey);

        kafka.AddProducer(producer => producer.WithKey<ProtoInvoiceCreationKey>()
            .WithValue<ProtoInvoiceCreationValue>()
            .WithConfiguration(configuration.GetSection("InvoiceCreated"))
            .SerializeKeyWithProto()
            .SerializeValueWithProto()
            .WithOutbox());

        kafka.AddProducer(producer => producer
            .WithKey<ProtoAccountCreationKey>()
            .WithValue<ProtoAccountCreationValue>()
            .WithConfiguration(configuration.GetSection("AccountCreated"))
            .SerializeKeyWithProto()
            .SerializeValueWithProto()
            .WithOutbox());

        return kafka;
    }
}