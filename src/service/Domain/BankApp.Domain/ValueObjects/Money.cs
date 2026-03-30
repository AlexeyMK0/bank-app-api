namespace Lab1.Domain.ValueObjects;

public record Money
{
    public static Money Zero => new Money(0);

    public Money(decimal value)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(value);
        Value = value;
    }

    public decimal Value { get; }

    public Money DecreaseBy(Money amount)
    {
        return new Money(Value - amount.Value);
    }

    public Money IncreaseBy(Money amount)
    {
        return new Money(Value + amount.Value);
    }
}