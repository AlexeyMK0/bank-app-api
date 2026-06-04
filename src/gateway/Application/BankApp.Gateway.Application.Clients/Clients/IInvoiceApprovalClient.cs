using BankApp.Gateway.Application.Abstractions.Requests.Approval;

namespace BankApp.Gateway.Application.Abstractions.Clients;

public interface IInvoiceApprovalClient
{
    Task ApproveInvoiceAsync(ApproveInvoice.Request request, CancellationToken cancellationToken);

    Task DeclineInvoiceAsync(DeclineInvoice.Request request, CancellationToken cancellationToken);

    Task AssignAccountantAsync(AssignAccountant.Request request, CancellationToken cancellationToken);
}