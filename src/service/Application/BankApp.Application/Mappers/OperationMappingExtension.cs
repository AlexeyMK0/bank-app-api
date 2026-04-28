using BankApp.Application.Contracts.OperationHistory;
using BankApp.Application.Mappers.OperationMappers;
using BankApp.Domain.Operations;
using BankApp.Domain.Operations.Implementation;

namespace BankApp.Application.Mappers;

public static class OperationMappingExtension
{
    public static OperationDto MapToDto(this OperationRecord record)
    {
        return record switch
        {
            DepositOperationRecord depositRecord => depositRecord.MapImplToDto(),
            PayInvoiceOperationRecord payInvoiceRecord => payInvoiceRecord.MapImplToDto(),
            PaymentReceivedOperationRecord paymentReceivedRecord => paymentReceivedRecord.MapImplToDto(),
            WithdrawOperationRecord withdrawRecord => withdrawRecord.MapImplToDto(),
            _ => throw new Exception(),
        };
    }
}