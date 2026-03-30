namespace Lab1.Domain.ValueObjects;

public record OperationRecordId(long Value)
{
    public static OperationRecordId Default => new OperationRecordId(-1);
}