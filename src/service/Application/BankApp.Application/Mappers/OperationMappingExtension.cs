using Contracts.OperationHistory;
using Lab1.Application.Mappers.OperationMappers;
using Lab1.Domain.Operations;
using Lab1.Domain.Operations.Implementation;

namespace Lab1.Application.Mappers;

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