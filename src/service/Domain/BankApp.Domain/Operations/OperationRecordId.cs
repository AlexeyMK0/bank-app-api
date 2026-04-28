namespace BankApp.Domain.Operations;

public record OperationRecordId(long Value)
{
    public static OperationRecordId Default => new OperationRecordId(-1);
}