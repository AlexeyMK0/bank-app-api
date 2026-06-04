using BankApp.Application.Contracts.Invoices;
using BankApp.Application.Contracts.Invoices.Operations;
using Itmo.Dev.Platform.Kafka.Consumer;
using Microsoft.Extensions.Logging;

namespace BankApp.Presentation.Kafka.Handlers;

public sealed class ApprovalResultKafkaHandler : IKafkaConsumerHandler<ProtoApprovalResultKey, ProtoApprovalResultValue>
{
    private readonly IInvoiceService _invoiceService;
    private readonly ILogger<ApprovalResultKafkaHandler> _logger;

    public ApprovalResultKafkaHandler(IInvoiceService invoiceService, ILogger<ApprovalResultKafkaHandler> logger)
    {
        _invoiceService = invoiceService;
        _logger = logger;
    }

    public async ValueTask HandleAsync(IEnumerable<IKafkaConsumerMessage<ProtoApprovalResultKey, ProtoApprovalResultValue>> messages, CancellationToken cancellationToken)
    {
        foreach (IKafkaConsumerMessage<ProtoApprovalResultKey, ProtoApprovalResultValue> message in messages)
        {
            switch (message.Value.Status)
            {
                case ProtoApprovalStatus.Approved:
                    await HandleApproveAsync(message.Value.InvoiceId, cancellationToken);
                    break;
                case ProtoApprovalStatus.Declined:
                    await HandleDeclinedAsync(message.Value.InvoiceId, cancellationToken);
                    break;
                default:
                    _logger.LogError("Unexpected approval status: {Status}", message.Value.Status);
                    break;
            }
        }
    }

    private async ValueTask HandleApproveAsync(long invoiceId, CancellationToken cancellationToken)
    {
        var request = new ApproveInvoice.Request(invoiceId);
        await _invoiceService.ApproveInvoicesAsync(request, cancellationToken);
    }

    private async ValueTask HandleDeclinedAsync(long invoiceId, CancellationToken cancellationToken)
    {
        var request = new DeclineInvoice.Request(invoiceId);
        await _invoiceService.DeclineInvoicesAsync(request, cancellationToken);
    }
}