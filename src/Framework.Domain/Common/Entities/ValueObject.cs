namespace Framework.Domain.Common.Entities;

/// <summary>
/// Base class for value objects - immutable objects defined by their attributes
/// </summary>
public abstract class ValueObject : IEquatable<ValueObject>
{
    /// <summary>
    /// Gets the atomic values that define equality for this value object
    /// </summary>
    protected abstract IEnumerable<object?> GetEqualityComponents();

    public override bool Equals(object? obj)
    {
        if (obj is null || obj.GetType() != GetType())
            return false;

        return Equals((ValueObject)obj);
    }

    public bool Equals(ValueObject? other)
    {
        if (other is null)
            return false;

        return GetEqualityComponents()
            .SequenceEqual(other.GetEqualityComponents());
    }

    public override int GetHashCode()
    {
        return GetEqualityComponents()
            .Select(x => x?.GetHashCode() ?? 0)
            .Aggregate((x, y) => x ^ y);
    }

    public static bool operator ==(ValueObject? left, ValueObject? right)
    {
        if (left is null && right is null)
            return true;

        if (left is null || right is null)
            return false;

        return left.Equals(right);
    }

    public static bool operator !=(ValueObject? left, ValueObject? right)
    {
        return !(left == right);
    }
}

/// <summary>
/// Base class for single-value objects (wrapper value objects)
/// </summary>
/// <typeparam name="TValue">The type of the wrapped value</typeparam>
public abstract class SingleValueObject<TValue> : ValueObject
    where TValue : notnull
{
    public TValue Value { get; }

    protected SingleValueObject(TValue value)
    {
        Value = value;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value.ToString() ?? string.Empty;

    public static implicit operator TValue(SingleValueObject<TValue> valueObject) => valueObject.Value;
}
