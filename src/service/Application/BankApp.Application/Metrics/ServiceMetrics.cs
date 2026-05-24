using BankApp.Application.Abstractions.Metrics;
using System.Diagnostics.Metrics;

namespace BankApp.Application.Metrics;

public class ServiceMetrics : IServiceMetrics
{
    public static Meter Meter { get; } = new("BankApp.Service");

    private readonly Counter<long> _createdAccountsCounter = Meter
        .CreateCounter<long>("bank_app_service_accounts_created_total");

    private readonly Counter<double> _withdrawalAmountCounter = Meter
        .CreateCounter<double>("bank_app_service_withdrawal_amount_total");

    private readonly Counter<double> _depositAmountCounter = Meter
        .CreateCounter<double>("bank_app_service_deposit_amount_total");

    private readonly Counter<long> _createdInvoicesCounter = Meter
        .CreateCounter<long>("bank_app_service_invoices_created_total");

    private readonly Counter<long> _cancelledInvoicesCounter = Meter
        .CreateCounter<long>("bank_app_service_invoices_cancelled_total");

    private readonly Counter<long> _paidInvoicesCounter = Meter
        .CreateCounter<long>("bank_app_service_invoices_paid_total");

    private readonly Counter<double> _invoicesAmountCounter = Meter
        .CreateCounter<double>("bank_app_service_invoices_amount_total");

    public void IncCreatedAccounts()
    {
        _createdAccountsCounter.Add(1);
    }

    public void IncWithdrawalAmount(decimal amount)
    {
        _withdrawalAmountCounter.Add((double)amount);
    }

    public void IncDepositAmount(decimal amount)
    {
        Console.WriteLine("aaaaaaaaa DepositAmount called");
        _depositAmountCounter.Add((double)amount);
    }

    public void IncCreatedInvoices()
    {
        _createdInvoicesCounter.Add(1);
    }

    public void IncPaidInvoices()
    {
        _paidInvoicesCounter.Add(1);
    }

    public void IncCancelledInvoices()
    {
        _cancelledInvoicesCounter.Add(1);
    }

    public void IncInvoiceTotalAmount(decimal amount)
    {
        _invoicesAmountCounter.Add((double)amount);
    }
}