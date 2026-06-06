namespace BankApp.Gateway.Presentation.Http.Operations.Accounts.Requests;

public sealed class GetUserAccountsRequest
{
    public int? PageSize { get; set; }

    public string? PageToken { get; set; }
}