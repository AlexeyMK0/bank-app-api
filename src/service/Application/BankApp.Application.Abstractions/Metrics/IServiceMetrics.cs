namespace BankApp.Application.Abstractions.Metrics;

public interface IServiceMetrics
{
    void IncCreatedAccounts();

    void IncWithdrawalAmount(decimal amount);

    void IncDepositAmount(decimal amount);

    void IncCreatedInvoices();

    void IncPaidInvoices();

    void IncCancelledInvoices();

    void IncInvoiceTotalAmount(decimal amount);
}