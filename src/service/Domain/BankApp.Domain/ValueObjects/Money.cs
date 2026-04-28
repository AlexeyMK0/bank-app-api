namespace BankApp.Domain.ValueObjects;

public record Money : IComparable<Money>
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

    public int CompareTo(Money? other)
    {
        if (ReferenceEquals(this, other)) return 0;
        if (other is null) return 1;
        return Value.CompareTo(other.Value);
    }
}