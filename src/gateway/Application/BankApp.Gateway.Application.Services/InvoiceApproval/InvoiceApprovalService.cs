using BankApp.Gateway.Application.Abstractions.Clients;
using BankApp.Gateway.Application.Abstractions.Requests;
using BankApp.Gateway.Application.Abstractions.Requests.Approval;
using BankApp.Gateway.Application.Contracts;

namespace BankApp.Gateway.Application.Services.InvoiceApproval;

public class InvoiceApprovalService : IInvoiceApprovalService
{
    private readonly IUserClient _userClient;
    private readonly IInvoiceApprovalClient _approvalClient;

    public InvoiceApprovalService(IInvoiceClient client, IUserClient userClient, IInvoiceApprovalClient approvalClient)
    {
        _userClient = userClient;
        _approvalClient = approvalClient;
    }

    public async Task ApproveInvoiceAsync(Guid userExternalId, long invoiceId, CancellationToken cancellationToken)
    {
        GetUser.Response getUserResponse = await _userClient.GetUserAsync(new GetUser.Request(userExternalId), cancellationToken);
        long userId = getUserResponse.UserId;

        var request = new ApproveInvoice.Request(invoiceId, userId);
        await _approvalClient.ApproveInvoiceAsync(request, cancellationToken);
    }

    public async Task DeclineInvoiceAsync(Guid userExternalId, long invoiceId, CancellationToken cancellationToken)
    {
        GetUser.Response getUserResponse = await _userClient.GetUserAsync(new GetUser.Request(userExternalId), cancellationToken);
        long userId = getUserResponse.UserId;

        var request = new DeclineInvoice.Request(invoiceId, userId);
        await _approvalClient.DeclineInvoiceAsync(request, cancellationToken);
    }

    public async Task AssignAccountantAsync(Guid userId, long accountantId, long invoiceId, CancellationToken cancellationToken)
    {
        var protoRequest = new AssignAccountant.Request(invoiceId, accountantId);
        await _approvalClient.AssignAccountantAsync(protoRequest, cancellationToken);
    }
}