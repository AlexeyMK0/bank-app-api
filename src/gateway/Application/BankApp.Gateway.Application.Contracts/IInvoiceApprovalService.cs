namespace BankApp.Gateway.Application.Contracts;

public interface IInvoiceApprovalService
{
    Task ApproveInvoiceAsync(
        Guid userExternalId,
        long invoiceId,
        CancellationToken cancellationToken);

    Task DeclineInvoiceAsync(
        Guid userExternalId,
        long invoiceId,
        CancellationToken cancellationToken);

    Task AssignAccountantAsync(
        Guid userId,
        long accountantId,
        long invoiceId,
        CancellationToken cancellationToken);
}