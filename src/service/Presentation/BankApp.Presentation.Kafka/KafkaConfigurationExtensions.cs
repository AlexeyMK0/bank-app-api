using BankApp.Presentation.Kafka.Handlers;
using Itmo.Dev.Platform.Kafka.Configuration;
using Itmo.Dev.Platform.Kafka.Extensions;
using Microsoft.Extensions.Configuration;

namespace BankApp.Presentation.Kafka;

public static class KafkaConfigurationExtensions
{
    public static IKafkaConfigurationBuilder AddPresentationConsumers(
        this IKafkaConfigurationBuilder kafka,
        IConfiguration configuration)
    {
        const string consumerKey = "Presentation:Kafka:Consumers";
        configuration = configuration.GetSection(consumerKey);

        kafka.AddConsumer(consumer => consumer
            .WithKey<ProtoApprovalResultKey>()
            .WithValue<ProtoApprovalResultValue>()
            .WithConfiguration(configuration.GetSection("ApprovalResult"))
            .DeserializeKeyWithProto()
            .DeserializeValueWithProto()
            .HandleWith<ApprovalResultKafkaHandler>());
        return kafka;
    }
}