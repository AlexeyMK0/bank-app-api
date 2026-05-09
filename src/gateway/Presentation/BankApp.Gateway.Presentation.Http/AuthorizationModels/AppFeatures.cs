#pragma warning disable SK1101
#pragma warning disable SK1400

namespace BankApp.Gateway.Presentation.Http.AuthorizationModels;

public static class AppFeatures
{
    public const string ReadAccount = "account:read";
    public const string ReadAccountBalance = "account:read_balance";
    public const string CreateAccount = "account:create";
    public const string AccountDeposit = "account:deposit";
    public const string AccountWithdraw = "account:withdraw";

    public const string CreateInvoice = "invoice:create";
    public const string ReadInvoice = "invoice:read";
    public const string CancelInvoice = "invoice:cancel";
    public const string PayInvoice = "invoice:pay";

    public const string ReadOperation = "operation:read";
}